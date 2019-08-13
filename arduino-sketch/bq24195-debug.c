#include <SPI.h>
#include <WiFiNINA.h>
#include <WiFiUdp.h>

#import "Wire.h"

WiFiServer server(3360);

#include "wiring_private.h"

#define PMIC_ADDRESS  0x6B

void setup() {
  pinMode(LED_BUILTIN, OUTPUT);
  
  //Initialize serial and wait for port to open:
  Serial.begin(1000000);
  int x = 0;
  while (!Serial && x < 30) {
    delay(100);
    x++;
  }

  if (WiFi.status() == WL_NO_MODULE) {
    if (Serial) Serial.println("Communication with WiFi module failed!");
    // don't continue
    while (true);
  }

  String fv = WiFi.firmwareVersion();
  if (fv < WIFI_FIRMWARE_LATEST_VERSION) {
    if (Serial) Serial.println("Please upgrade the firmware");
  }

  WiFi.noLowPowerMode();
  
retry:
  if (Serial) Serial.println("Connecting to wifi...");
  
  uint8_t mac[6];
  WiFi.macAddress(mac);
  
  WiFi.setHostname("diag"); // note: this seems to do nothing..

  int status = WiFi.begin("WIFI_NAME_HERE", "WIFI_PASSWORD_HERE");
  if (status == WL_CONNECTED) {
    if (Serial) Serial.println("Connected! IP address:");
    if (Serial) Serial.println(WiFi.localIP());
  } else if (status == WL_IDLE_STATUS) {
    if (Serial) Serial.println("Not connected :'(");
    delay(5000);
    goto retry;
  }

  Wire.begin();
  server.begin();
}

byte getReg(byte reg) {
  
  Wire.beginTransmission(PMIC_ADDRESS);
  
  Wire.write((byte)reg);
  
  Wire.endTransmission();
  
  Wire.beginTransmission(PMIC_ADDRESS);
  
  Wire.requestFrom(PMIC_ADDRESS, 1);
  
  Wire.endTransmission();
  
  byte val = Wire.read();
  
  return val;
}

void writeReg(byte reg, byte val) {
  Wire.beginTransmission(PMIC_ADDRESS);
  Wire.write(reg);
  Wire.write(val);
  Wire.endTransmission();
}

void loop() { 
  WiFiClient client = server.available();
  byte reg, regVal, newval, ishigh;
  int batt, pinhigh;

  if (client) {
    if(Serial)Serial.println("got client");
    while (client.connected()) {
      if (client.available()) {
        if(Serial)Serial.println("got data");
        byte c = client.read();
        switch (c)
        {
          case 0x01: // get register
            reg = client.read();
            regVal = getReg(reg);
            client.write(0x02);
            client.write(regVal);
            if(Serial)Serial.println("written 2 bytes back");
          break;
          case 0x03: // get battery adc
            batt = analogRead(ADC_BATTERY);
            client.write(0x04);
            client.write((byte)(batt >> 8));
            client.write((byte)(batt & 0xff));
          break;
          case 0x05: // get usb host enable
            pinhigh = digitalRead(PIN_USB_HOST_ENABLE);
            client.write(0x06);
            client.write((byte)(pinhigh == HIGH ? 1 : 0));
          break;
          case 0x07: // write register
            reg = client.read();
            newval = client.read();
            writeReg(reg, newval);
            client.write(0x08);
          break;
          case 0xa: // set host enable
            ishigh = client.read();
            pinMode(PIN_USB_HOST_ENABLE, OUTPUT);
            digitalWrite(PIN_USB_HOST_ENABLE, ishigh ? HIGH : LOW);
            client.write(0x0b);
          break;
        }
      }
    }
    client.stop();
  }
}