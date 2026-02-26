/*
  Program Name: IoT Project 1
  Date: 24th February 2026
  modified by Arif on 26th February 2026
  Purpose: Rain detector with alerts for start / stop.
*/

const int pinRainSensor = A0;
const int pinTempSensor = D2;
const int pinLightSensor = A1;
const int pinLED = D4;
const int pinButton = D8;

// Needed for Temp sensor
//#include "DHT.h"
//#define DHTPIN 2     // what pin we're connected to
//#define DHTTYPE DHT11   // DHT 11 
//DHT dht(DHTPIN, DHTTYPE);

void setup() {
  // init the serial port
  Serial.begin(9600);
  Serial.println("Setup starting...");

  // set pin modes
  pinMode(pinRainSensor, INPUT);
  pinMode(pinTempSensor, INPUT);
  pinMode(pinLightSensor, INPUT);
  pinMode(pinButton, INPUT);
  pinMode(pinLED, OUTPUT);

  // initiate the temp sensor
  dht.begin();

  Serial.println("Setup finished");
}

int rainVal, tempVal, lightVal;
void loop() {
  // read the rain sensor
  rainVal = analogRead(pinRainSensor);
  Serial.print("Rain: ");
  Serial.println(rainVal);

  // read the temp sensor
  tempVal = analogRead(pinTempSensor);
  Serial.print("Temp: ");
  Serial.print(tempVal);
  Serial.println("C");

  // read the light sensor
  lightVal = analogRead(pinLightSensor);
  Serial.print("Light: ");
  Serial.println(lightVal);

  // detect button press
  if (digitalRead(pinButton)>0) {
    Serial.println("BUTTON Pressed!");
  }

  // Test: Set the LED on
  if (lightVal>200) {
    digitalWrite(pinLED, HIGH);
  } else {
    digitalWrite(pinLED, LOW);
  }

  delay(500);
  Serial.println("=================");
}
