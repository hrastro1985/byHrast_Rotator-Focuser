
// Import required libraries
#include <AccelStepper.h>

/////// rezolucija enkodera  2548 imp/okr

///////////////////////////////////////////////////Rotator varijable ////////////////////////////////////////////////////////////////////////////////

int RmotorInterfaceType = 1;
int RmotorSpeed = 800;
int RmotorAccel = 1000;

int RstepPin = 4;
int RdirPin = 5;
const int RMS0 = 21;
const int RMS1 = 19;
const int RMS2 = 18;
const int RVmotEn = 22;

long Rpos = 0;
float deg = 0;
bool RIsMoving = false;
bool Rhold = false;


// Rotator varijable
int microRstepPing = 32;
float Lpulley = 180; // broj zubi velike remenice
float Spulley = 16; // broj zubi male remenice
float numStep = 400; // broj stepova motora
float resolution = (360 / (numStep * ((Lpulley / Spulley) * microRstepPing))) * 4; // rezolucija stepa u deg -- ispadne 0,04deg po impulsu

AccelStepper Rstepper(RmotorInterfaceType, RstepPin, RdirPin);

bool dir = true;

///////////////////////////////////////////////////Fokuser varijable ////////////////////////////////////////////////////////////////////////////////


int FmotorInterfaceType = 1;
int FmotorSpeed = 2000;
int FmotorAccel = 1000;

int FstepPin = 23;
int FdirPin = 25;
const int FMS0 = 27;
const int FMS1 = 32;
const int FMS2 = 33;
const int FVmotEn = 26;

//int FstepPin = 5;
//int FdirPin = 4;
//const int FMS0 = 21;
//const int FMS1 = 19;
//const int FMS2 = 18;
//const int FVmotEn = 22;

long Fpos = 0;
bool FIsMoving = false;

AccelStepper Fstepper(FmotorInterfaceType, FstepPin, FdirPin);



void setup() {

  Serial.begin(9600);

  /////////////////////////// Rotator dio /////////////////////////////////

  Rstepper.setMaxSpeed(RmotorSpeed);
  Rstepper.setSpeed(RmotorSpeed);
  Rstepper.setAcceleration(RmotorAccel);
  Rstepper.setEnablePin(RVmotEn);
  Rstepper.setPinsInverted(false, false, false);


  pinMode (RMS0, OUTPUT);
  pinMode (RMS1, OUTPUT);
  pinMode (RMS2, OUTPUT);
  pinMode (RVmotEn, OUTPUT);

  digitalWrite(RMS0, HIGH);
  digitalWrite(RMS1, HIGH);
  digitalWrite(RMS2, HIGH);
  digitalWrite(RVmotEn, HIGH);

  /////////////////////////// Fokuser dio /////////////////////////////////

  Fstepper.setMaxSpeed(FmotorSpeed);
  Fstepper.setSpeed(FmotorSpeed);
  Fstepper.setAcceleration(FmotorAccel);
  Fstepper.setEnablePin(FVmotEn);
  Fstepper.setPinsInverted(true, false, false);


  pinMode (FMS0, OUTPUT);
  pinMode (FMS1, OUTPUT);
  pinMode (FMS2, OUTPUT);
  pinMode (FVmotEn, OUTPUT);

  digitalWrite(FMS0, HIGH);
  digitalWrite(FMS1, LOW);
  digitalWrite(FMS2, LOW);
  digitalWrite(FVmotEn, HIGH);
   
    Fstepper.moveTo(2000); // samo odglumim da imde na 2000
    Fstepper.run();
}

void loop() {
  SerCommunication();

  /////////////////////////// Rotator dio /////////////////////////////////

  while (Rpos != Rstepper.currentPosition()) {
    digitalWrite(RVmotEn, LOW); /// aktiviram motor
    RIsMoving = true;
    Rstepper.moveTo(Rpos);
    Rstepper.run();
    SerCommunication();
  }
  if (Rhold == false) digitalWrite(RVmotEn, HIGH); /// deaktiviram motor i deaktiviram motor samo ako je Rhold == false
  RIsMoving = false;

  /////////////////////////// Fokuser dio /////////////////////////////////

  while (Fpos != Fstepper.currentPosition()) {
    digitalWrite(FVmotEn, LOW); /// aktiviram motor
    FIsMoving = true;
    Fstepper.moveTo(Fpos);
    Fstepper.run();
    SerCommunication();
  }
  digitalWrite(FVmotEn, HIGH); /// deaktiviram motor
  FIsMoving = false;
}

//////////////////////////////////////////////////////////////////////////

void SerCommunication () {
  if (Serial.available()) {
    String incoming = Serial.readStringUntil('#');
    String type = incoming.substring(0, 1);
    String command = incoming.substring(1, 2);
    float payload = incoming.substring(2, 10).toFloat();


    /////////////////////////// Rotator dio /////////////////////////////////
    if (type.equals("R")) { /// ako je tip uređaja rotator

      if (command.equals("Q")) { ///Q hitno zaustavljanje
        Rstepper.stop();   // hitno zaustavi
        Rpos = Rstepper.currentPosition();
        Serial.print("#");
      }
      else if (command.equals("W")) { ///mijenjam smjer vrtnje rotatora
        Rstepper.setPinsInverted(true, false, false);
        dir = false;
        Serial.print("CW#");
      }
      else if (command.equals("C")) { ///mijenjam smjer vrtnje rotatora
        Rstepper.setPinsInverted(false, false, false);
        dir = true;
        Serial.print("CCW#");
      }
      else if (command.equals("A")) { ///A kao apsolutni pomak odnosno do kuda da vrti
        DegToRpos(payload);
        Serial.print(payload);
        Serial.print("#");
      }
      else if (command.equals("R")) { ///A kao apsolutni pomak odnosno do kuda da vrti
        deg = payload;
        float Fpos = Rstepper.currentPosition();
        Fpos = Fpos * resolution;
        deg = deg + Fpos;
        DegToRpos(deg);
        Serial.print(payload);
        Serial.print("#");
      }
      else if (command.equals("P")) { ///Vraca trenutnu poziciju
        float Fpos = Rstepper.currentPosition();
        Fpos = Fpos * resolution;
        Serial.print(Fpos);
        Serial.print("#");
      }
      else if (command.equals("M")) { ///Vrača 1 jel vozi
        Serial.print(RIsMoving);
        Serial.print("#");
      }
      else if (command.equals("S")) { /// Vraca rezoluciju
        Serial.print(String(resolution) + "#");
      }
      else if (command.equals("H")) { /// drzi motore stalno pod naponom
        Rhold = true;
        Serial.print("HoldOn#");
      }
      else if (command.equals("X")) { /// drzi motore stalno pod naponom
        Rhold = false;
        Serial.print("HoldOff#");
      }
      else if (command.equals("Y")) { /// skinkronizira poziciju
        Rstepper.setCurrentPosition(payload / resolution);
        Rpos = payload;
        Serial.print("#");
      }
      else if (command.equals("#")) { ///Vraca naziv
        Serial.print("RotatorByHrast#");
      }
      else {
        Serial.print("#");
      }
    }


    /////////////////////////// Fokuser dio /////////////////////////////////
    if (type.equals("F")) {
      if (command.equals("Q")) { ///Q hitno zaustavljanje
        Fstepper.stop();   // hitno zaustavi
        Serial.print("#");
      }

      else if (command.equals("R")) { ///R kao relativni pomak
        Fpos = payload + Fpos;  // nova pozicija je stara pozicija + dodatak
        Fstepper.move(Fpos);
        Serial.print("#");
      }
      else if (command.equals("A")) { ///A kao apsolutni pomak odnosno do kuda da vrti
        Fpos = payload;
        Fstepper.moveTo(Fpos);
        Serial.print("#");
      }
      else if (command.equals("P")) { ///Vrača trenutnu poziciju
        Serial.print(Fstepper.currentPosition());
        Serial.print("#");
      }
      else if (command.equals("M")) { ///Vrača 1 jel vozi
        Serial.print(FIsMoving);
        Serial.print("#");
      }
      else if (command.equals("#")) { ///Vrača naziv
        Serial.print("FocuserByHrast#");
      }
      else {
        Serial.print("#");
      }
    }
  }
}

void DegToRpos (float RposF) {
  RposF = RposF / resolution;
  Rpos = RposF;
  Rstepper.moveTo(Rpos);
}
