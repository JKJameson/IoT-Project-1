/*
  Program Name: IoT Project 1
  Date: 24th February 2026
  modified by Arif on 26th February 2026
  Purpose: Rain detector with alerts for start / stop.
*/
#include <math.h>

/*
  Universal 8bit Graphics Library (https://github.com/olikraus/u8g2/)
  Copyright (c) 2016, olikraus@gmail.com
  All rights reserved.

  Redistribution and use in source and binary forms, with or without modification, 
  are permitted provided that the following conditions are met:

  * Redistributions of source code must retain the above copyright notice, this list 
    of conditions and the following disclaimer.
    
  * Redistributions in binary form must reproduce the above copyright notice, this 
    list of conditions and the following disclaimer in the documentation and/or other 
    materials provided with the distribution.

  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND 
  CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
  INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF 
  MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
  DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR 
  CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
  SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT 
  NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; 
  LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER 
  CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, 
  STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
  ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF 
  ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.  
*/
#include <Arduino.h>
#include <U8g2lib.h>

#ifdef U8X8_HAVE_HW_SPI
#include <SPI.h>
#endif
#ifdef U8X8_HAVE_HW_I2C
#include <Wire.h>
#endif

U8G2_SSD1306_128X64_NONAME_F_HW_I2C u8g2(U8G2_R0, /* reset=*/ U8X8_PIN_NONE);

const int B = 3950;            // B value of the thermistor
const int R0 = 100000;   
const int pinRainSensor = A3;
const int pinTempSensor = A0;
const int pinLightSensor = A1;
const int pinLED = D4;
const int pinButton = D8;
const int RAIN_SENSOR_MIN = 600;

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

  u8g2.begin();

  Serial.println("Setup finished");
}

int rainVal, tempVal, lightVal;
bool isRaining = false;
bool isNight = false;
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

  // is it raining?
  if (rainVal<RAIN_SENSOR_MIN) {
    isRaining = true;
  } else {
    isRaining = false;
  }

  if (isRaining) {
    // turn LED on
    digitalWrite(pinLED, HIGH);
  } else {
    // turn LED off
    digitalWrite(pinLED, LOW);
  }

  Serial.println("=================");

  u8g2.clearBuffer();					// clear the internal memory
  u8g2.setFont(u8g2_font_ncenB08_tr);	// choose a suitable font
  u8g2.drawStr(0,10,"Temp: ");	// write something to the internal memory

  // convert temp float to char[]
  char buf[16];  // big enough for "-123.45\0" etc.
  dtostrf(temperature, 0, 1, buf);  // width 0, 1 decimal place

  u8g2.drawStr(40,10,buf);	// write something to the internal memory
  u8g2.drawStr(60,10,"C"); // temp unit

  if (lightVal>100) {
    isNight = false;
  } else {
    isNight = true;
  }

  // say if it is raining or not
  if (isRaining) {
    if (!isNight) {
      // TODO: Detect a raise in brightness and if the rain sensor is a steady or declining value, the weather has changed from rain to clear/sunny.

    }
    u8g2.drawStr(0,20,"It is raining :-(");
  } else {
    u8g2.drawStr(0,20,"It is not raining :-)");
  }

  // light sensor line
  
  char bufLight[32];
  sprintf(bufLight, "Light Sensor: %d", lightVal);
  u8g2.drawStr(0,40,bufLight);

  u8g2.sendBuffer();					// transfer internal memory to the display
  delay(100);  
}
