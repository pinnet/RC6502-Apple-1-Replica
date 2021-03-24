#include <BasicTerm.h>
#include <Arduino.h>
#include <MCP23S17.h>
#include <SPI.h>
#include "RTClib.h"
#include <Wire.h>

#define DEBUG 1
#define KBD_INTERRUPT_ENABLE true
#define KBD_SEND_TIMEOUT 23
#define IO_SS 10
#define IO_VIDEO 0
#define IO_VIDEO_D0 0
#define IO_VIDEO_D6 6
#define VIDEO_RDA 5
#define VIDEO_DA 3
#define IO_KBD 1
#define IO_KBD_D0 8
#define IO_KBD_D6 14
#define IO_KBD_DA 15
#define KBD_READY 2
#define KBD_STROBE 4

RTC_DS1307 rtc;
static char Hr[3]  = {'0','0','\0'};
static char Min[3] = {'0','0','\0'};
static char Sec[3] = {'0','0','\0'};
static int lastSecond = 0;
char daysOfTheWeek[7][12] = {"Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"};

BasicTerm term(&Serial);
static int curX = 0;
static int curY = 0;

MCP23S17 bridge(&SPI, IO_SS, 0);

void setup() {
  Serial.begin(115200);
  term.init();
  term.cls();
  DateTime now = rtc.now();
  if (! rtc.isrunning() || now.year() <= 2000) {
    rtc.adjust(setTime());
  }
  //rtc.adjust(setTime());
  configure_pins();
  configure_bridge();
  output_status();
}
DateTime setTime(){
  
  term.position(curX,curY);
  
  bool correct = false;
  int day,month,year,hour,minute,seconds;
  do{ 
      term.position(0,0);     
      term.println("Time not set");
      term.print(" Day:");    curX = 1; curY = 5;
      day = inputNumber(1,31);
      term.print(" Month:");  curX = 2; curY = 7;
      month = inputNumber(1,12);
      term.print(" Year:");   curX = 3; curY = 6;
      year = inputNumber(1999,2999);
      term.print(" Hour:");   curX = 4; curY = 6;
      hour = inputNumber(0,23);      
      term.print(" Minute:"); curX = 5; curY = 7;
      minute = inputNumber(0,60);
      term.print(" Second:"); curX = 6; curY = 8;
      seconds = inputNumber(0,59);
      term.print("\n is that correct?");
      correct = inputCorrect();
      term.cls();
      }
  while(!correct);
  return DateTime(year,month,day,hour,minute,seconds);
}

int inputNumber(int min,int max)
{ 
  bool validated = false;
  int outint = 0; 
  do{  
      term.flush();
      char chrin = '\0';
      String strin ="";

      do{
        if(Serial.available() > 0)
        {
          chrin = term.read();
          term.print(chrin);
          if (chrin >= '0' && chrin <= '9')      
              strin += String(chrin);
        }
      }
      while((uint8_t)chrin != 13);
      
      strin.trim();
      outint = strin.toInt(); 
      if (outint < min || outint > max){
          term.position(10,0);
          
          term.set_color(BT_RED,BT_WHITE);
          term.set_attribute(BT_REVERSE);
          term.print(" INVALID NUMBER must be between " + String(min) + " & " + String(max));
          term.set_attribute(BT_NORMAL);
          term.position(curX,curY);
          term.print(F("                            "));
          term.position(curX,curY);
      }
      else
      {   
          term.position(10,0);
          term.print(F("                                                           "));
          term.position(curX,curY);
          validated = true;
      }
  }
  while (!validated);
  term.println();   
  return outint;
  
}
bool inputCorrect(){

  while (term.available() <= 0) {
  }
  char charin = term.read();
  if (charin =='Y' || charin =='y') 
  return true;
  else
  return false;

}
void configure_pins() {
  pinMode(KBD_READY, INPUT);
  pinMode(VIDEO_DA, INPUT);
  pinMode(KBD_STROBE, OUTPUT);
  pinMode(VIDEO_RDA, OUTPUT);
}

String getTime(){
  DateTime now = rtc.now();
  sprintf(Hr,"%02d",now.hour());
  sprintf(Min,"%02d",now.minute());
  sprintf(Sec,"%02d",now.second());
  return String(Hr)+":"+String(Min)+":"+String(Sec);
}
String getDay(){
  DateTime now = rtc.now();
  return daysOfTheWeek[now.dayOfTheWeek()];
}
String getDate(){
  DateTime now = rtc.now();
  return String(now.day()) + "/" + String(now.month()) + "/" + String(now.year());
}

void configure_bridge() {
  bridge.begin();
  /* Configure video section */
  for (int i = IO_VIDEO_D0; i <= IO_VIDEO_D6; i++) {
    bridge.pinMode(i, INPUT);
  }
  bridge.pinMode(7, INPUT_PULLUP);
  /* Configure keyboard section */
  for (int i = 8; i <= 15; i++) {
    bridge.pinMode(i, OUTPUT);
  }
}

void output_status() {
  curX = 1; curY = 0;
  debug_value("Video DA", digitalRead(VIDEO_DA));
  debug_value("Video D0-D6", bridge.readPort(IO_VIDEO) & 127);
  debug_value("Keyboard RDY", digitalRead(KBD_READY));
}

void debug_value(String description, byte value) {
  debug_value(description, value, 1);
}

void debug_value(String description, byte value, int level) {
  if (DEBUG < level) return;
  term.print(description);
  term.print(": ");
  print_hex(value);
}

void print_hex(byte value) {
  print_hex(value, true);
}

void print_hex(byte value, bool newline) {
  if (value <= 0xF) {
    term.print("0x0");
  } else {
    term.print("0x");
  }
  
  if (newline) term.println(value, HEX);
  else term.print(value, HEX);
}

void serial_receive() {
  if (term.available() > 0) {
    
    term.position(0,40);
       
    int c = term.read();
    term.print(c);
    term.position(curX,curY);
    //term.print(String(c));
    //debug_value("PIA RX", c, 10);
    pia_send(c);
    
  }
}

void pia_send(int c) {
  /* Make sure STROBE signal is off */
  digitalWrite(KBD_STROBE, LOW);
  c = map_to_ascii(c);

  /* Output the actual keys as long as it's supported */
  if (c < 96) {
    bridge.writePort(IO_KBD, c | 128);

    digitalWrite(KBD_STROBE, HIGH);
    if (KBD_INTERRUPT_ENABLE) {
      byte timeout;

      /* Wait for KBD_READY (CA2) to go HIGH */
      timeout = KBD_SEND_TIMEOUT;
      while(digitalRead(KBD_READY) != HIGH) {
        delay(1);
        if (timeout == 0) break;
        else timeout--;
      }
      digitalWrite(KBD_STROBE, LOW);

      /* Wait for KBD_READY (CA2) to go LOW */
      timeout = KBD_SEND_TIMEOUT;
      while(digitalRead(KBD_READY) != LOW) {
        delay(1);
        if (timeout == 0) break;
        else timeout--;
      }
    } else {
      delay(KBD_SEND_TIMEOUT);
      digitalWrite(KBD_STROBE, LOW);
    }
  }
}

char map_to_ascii(int c) {
  /* Convert ESC key */
  if (c == 203) {
    c = 27;
  }

  /* Ctrl A-Z */
  if (c > 576 && c < 603) {
    c -= 576;
  }

  /* Convert lowercase keys to UPPERCASE */
  if (c > 96 && c < 123) {
    c -= 32;
  }
  
  return c;
}

void serial_transmit() {
  digitalWrite(VIDEO_RDA, HIGH);

  if (digitalRead(VIDEO_DA) == HIGH) {
    char c = bridge.readPort(IO_VIDEO) & 127;
    debug_value("PIA TX", c, 10);
    digitalWrite(VIDEO_RDA, LOW);

    delay(12);
    send_ascii(c);
  }
}

void send_ascii(char c) {
  if (DEBUG >= 5) term.print("[");
  switch (c) {
    case 0x0d: 
          term.println(); /* Replace CR with LF */
          curX ++;
          curY = 0;
          break;
    default:
      term.print(c);
      curY ++;
  }
  if (DEBUG >= 5) term.print("]");
}
void printHeader(){
  DateTime now = rtc.now();
  if(now.second() == lastSecond) return;
  lastSecond = now.second();
  term.show_cursor(false);
  term.position(0,0);
  term.set_color(BT_WHITE,BT_BLUE);
  String timeline = getDay() + " " + getDate() + " " + getTime() + " "; 
  String padding = "";
  for(int i = 22; i < 80 - timeline.length();i++){ padding += " ";}
  term.print(F("RC6502 Apple 1 Replica"));
  term.print(padding);
  term.position(0,80 - timeline.length());
  term.print(timeline);
  term.set_color(BT_YELLOW,BT_BLACK);
  term.position(curX,curY);
}

void loop() {
  serial_receive();
  serial_transmit();
  if(curY > 80){ curY=0;curX++;}
  printHeader();
  }
