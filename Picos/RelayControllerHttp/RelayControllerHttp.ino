#include <WiFi.h>
#include <HTTPClient.h>
#include <Arduino_JSON.h>
#include <string>


const char* ssid = "<InsertSSID>";
const char* password = "<InsertWPSPassword>";
String hostName = "RR_Cabinet      ";
String AuthKey = "<Insert_32_char_Shared_Key_Here>";
String serverAddress = "http://192.168.200.36:5000/";  // Replace with your server address

const int SolenoidColdKitchenPin = 17;   // Channel 5
const int SolenoidHotKitchenPin = 16;    // Channel 6
const int SolenoidColdBathroomPin = 19;  // Channel 3
const int SolenoidHotBathroomPin = 18;   // Channel 4 
const int SolenoidBoilerControlPin = 20; // Channel 2
const int PumpColdPin = 21;              // Channel 1
const int LEDPin = 13;                   // RGB LED
const int BuzzerPin = 6;                 // Buzzer

WiFiClient client;

void setup() {
  
  int LEDPinOnboard = 32; 
  pinMode(LEDPinOnboard, OUTPUT);
  digitalWrite(LEDPinOnboard, HIGH);

  Serial.begin(115200);
  WiFi.begin(ssid, password);

  while (WiFi.status() != WL_CONNECTED) 
  {
    delay(1000);
    Serial.println("Connecting to Wi-Fi...");
  }

  Serial.println("Wi-Fi connected successfully: " + WiFi.localIP().toString());
  pinMode(SolenoidColdKitchenPin, OUTPUT);
  pinMode(SolenoidHotKitchenPin, OUTPUT);
  pinMode(SolenoidColdBathroomPin, OUTPUT);
  pinMode(SolenoidHotBathroomPin, OUTPUT);
  pinMode(PumpColdPin, OUTPUT);
  pinMode(SolenoidBoilerControlPin, OUTPUT);
  pinMode(LEDPin, OUTPUT);
  pinMode(BuzzerPin, OUTPUT);
}

void loop() {
    fetchRelayStatus();
    delay(350);
}

void fetchRelayStatus() {
  HTTPClient http;
  String url = serverAddress + "api/calculations";  // Convert hostName to arduino::String
  String authHeader = hostName + AuthKey;
  // Serial.print("Connecting to url:");
  // Serial.println(url);
  // Perform HTTP request
  http.begin(url);
  //Serial.println(authHeader);
  http.addHeader("ServiceKey", authHeader);
  int httpResponseCode = http.GET();
  if (httpResponseCode == 200) 
  {
    digitalWrite(LEDPin, HIGH);
    JSONVar myObject = JSON.parse(http.getString());
    //Serial.print("HTTP Success! code: ");
    //Serial.println(httpResponseCode);
    //Serial.println(myObject);
    http.end();
    bool ICTIKO = myObject["IsColdTapInKitchenOn"];
    bool IHTIKO = myObject["IsHotTapInKitchenOn"]; 
    bool ICTIBO = myObject["IsColdTapInBathroomOn"];
    bool IHTIBO = myObject["IsHotTapInBathroomOn"]; 
    bool BSE = myObject["BoilerSolenoidEnabled"]; 
    bool PUMP = myObject["WaterPumpEnabled"]; 
    bool SHUTDOWN = myObject["WaterShutOff"]; 

    Serial.println("Got HTTP Data from Server");
    Serial.print("Kitchen Cold");Serial.println(ICTIKO);
    Serial.print("Kitchen Hot");Serial.println(IHTIKO);
    Serial.print("Bathroom Cold");Serial.println(ICTIBO);
    Serial.print("Bathroom Hot");Serial.println(IHTIBO);
    Serial.print("Boiler");Serial.println(BSE);
    Serial.print("Pump");Serial.println(PUMP); 
    Serial.print("SHUTDOWN");Serial.println(SHUTDOWN);

    digitalWrite(SolenoidColdKitchenPin, ICTIKO == true ? HIGH : LOW);
    digitalWrite(SolenoidHotKitchenPin, IHTIKO == true ? HIGH : LOW);
    digitalWrite(SolenoidColdBathroomPin, ICTIBO == true ? HIGH : LOW);
    digitalWrite(SolenoidHotBathroomPin,  IHTIBO == true ? HIGH : LOW);
    digitalWrite(SolenoidBoilerControlPin, BSE == true ? HIGH : LOW);
    digitalWrite(PumpColdPin, PUMP == true ? HIGH : LOW);
    digitalWrite(BuzzerPin, SHUTDOWN && PUMP == true ? HIGH : LOW);
  } 
  else 
  {
    // FailSafe
    digitalWrite(LEDPin, LOW);
    digitalWrite(SolenoidColdKitchenPin, LOW);
    digitalWrite(SolenoidHotKitchenPin, LOW);
    digitalWrite(SolenoidColdBathroomPin, LOW);
    digitalWrite(SolenoidHotBathroomPin, LOW);
    digitalWrite(SolenoidBoilerControlPin, LOW);
    digitalWrite(PumpColdPin, LOW);
    digitalWrite(BuzzerPin, LOW);
    Serial.print("HTTP error code: ");
    Serial.println(httpResponseCode);
    http.end();
  }
}