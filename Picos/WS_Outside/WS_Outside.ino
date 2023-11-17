#include <WiFi.h>
#include <HTTPClient.h>
#include <Arduino_JSON.h>
#include <Arduino.h>
#include <string>
#include "Ultrasonic.h"
#include <Grove_LED_Bar.h>

int LEDPin = 12;
int LEDOnboard = 25;
Ultrasonic ultrasonicFresh(7);     // Yellow to Blue Fresh Tank
Ultrasonic ultrasonicDirty(9);     // Yellow to Grey Waste Tank
Grove_LED_Bar barFresh(4, 5, 0);   // white, yellow
Grove_LED_Bar barGrey(10, 11, 1);  // white, yellow

float TankDepthInCM = 23;  // Size of Tank

String hostName = "TankSensors     ";

const char* ssid = "<InsertSSID>";
const char* password = "<InsertWPSPassword>";
String AuthKey = "<Insert_32_char_Shared_Key_Here>";
String serverAddress = "http://192.168.200.36:5000/";  // Replace with your server address

WiFiClient client;
HTTPClient http;
String authHeader;
String hostshort;

void setup(void) {
  pinMode(LEDPin, OUTPUT);
  int LEDPinOnboard = 32;
  pinMode(LEDPinOnboard, OUTPUT);
  digitalWrite(LEDPinOnboard, HIGH);
  authHeader = hostName + AuthKey;
  hostshort = hostName;
  hostshort.trim();
  Serial.println(authHeader);
  Serial.begin(115200);
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    digitalWrite(LEDPin, HIGH);
    delay(250);
    digitalWrite(LEDPin, LOW);
    delay(250);
    digitalWrite(LEDPin, HIGH);
    delay(250);
    Serial.println("Init >> Connecting to Wi-Fi...");
    digitalWrite(LEDPin, LOW);
    delay(250);
  }
  Serial.println("Init >> Wi-Fi connected successfully: " + WiFi.localIP().toString());
  digitalWrite(LEDPin, HIGH);
}
void loop() {
  digitalWrite(LEDPin, HIGH);
  float RangeInCentimeters;
  float RangeInCentimetersDirty;

  RangeInCentimeters = ultrasonicFresh.MeasureInCentimeters();
  RangeInCentimetersDirty = ultrasonicDirty.MeasureInCentimeters();

  if (RangeInCentimeters > TankDepthInCM) { RangeInCentimeters = TankDepthInCM; }
  if (RangeInCentimetersDirty > TankDepthInCM) { RangeInCentimetersDirty = TankDepthInCM; }
  float WaterDepth = TankDepthInCM - RangeInCentimeters;
  float WaterDepthD = TankDepthInCM - RangeInCentimetersDirty;
  float WaterPercent = (WaterDepth / TankDepthInCM) * 100.0;    // clean tank 100% is good (so distance should be nearer 23 cm)
  float WaterPercentD = (WaterDepthD / TankDepthInCM) * 100.0;  // Grey water tank should measure emptiness not fullness (i.e. 100% is bad, not good) (so distance should be nearer zero cm)
  int WaterDepthBar = (WaterPercent / 10);
  int WaterDepthBarD = (WaterPercentD / 10);

  JsonSendSensorData("CleanWaterTank", WaterPercent);
  barFresh.setLevel(WaterDepthBar);
  JsonSendSensorData("WasteWaterTank", WaterPercentD);
  barGrey.setLevel(WaterDepthBarD);

  delay(250);
  digitalWrite(LEDPin, LOW);
  delay(250);
}

void JsonSendSensorData(String Sensor, int value) {

  std::string valueStr = std::to_string(value);
  String arduinoValueStr = String(valueStr.c_str());
  String rawUrl = serverAddress + "api/analogs/" + Sensor + "/" + arduinoValueStr;
  http.begin(rawUrl);
  http.addHeader("ServiceKey", authHeader);
  if (http.GET() == 200) {
    Serial.println("HTTP Success -- Send: ");
    digitalWrite(LEDPin, HIGH);
  } else {
    Serial.println("HTTP error -- Send: ");
    digitalWrite(LEDPin, LOW);
  }
  Serial.print(Sensor);
  Serial.print("|");
  Serial.println(arduinoValueStr);
  http.end();
}