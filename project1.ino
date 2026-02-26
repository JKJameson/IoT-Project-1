/*
  Program Name: IoT Project 1
  Date: 24th February 2026
  modified by Arif on 26th February 2026
  Purpose: Rain detector with alerts for start / stop.
*/
#include <math.h>

const int B = 4275000;            // B value of the thermistor
const int R0 = 100000;   
const int pinRainSensor = A3;
const int pinTempSensor = A0;
const int pinLightSensor = A1;
const int pinLED = D4;
const int pinButton = D8;

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
  float R = 1023.0/tempVal-1.0;
    R = R0*R;

    float temperature = 1.0/(log(R/R0)/B+1/298.15)-273.15; // convert to temperature via datasheet
  
  Serial.print("Temp: ");
  Serial.print(temperature);
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
