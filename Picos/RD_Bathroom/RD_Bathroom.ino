#include <WiFi.h>
#include <HTTPClient.h>
#include <Arduino_JSON.h>
#include <Arduino.h>
#include <string>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>

#define SCREEN_WIDTH 128
#define SCREEN_HEIGHT 64

Adafruit_SSD1306 display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire);

const char* ssid = "<InsertSSID>";
const char* password = "<InsertWPSPassword>";

String hostName = "Bathroom        ";
String AuthKey = "<Insert_32_char_Shared_Key_Here>";
String serverAddress = "http://192.168.200.36:5000/";  // Replace with your server address

const int ColdButtonPin = 19;      // Cold Button = GPIO 16 on Pico (Grove D16)
const int ColdLEDPin = 18;         // Cold Button = GPIO 17 on Pico (Grove D16)
const int MainsWaterSelect = 16;   // Mains cold water and drainage in place
const int HotButtonPin = 21;       // Hot Button = GPIO 20 on Pico (Grove D20)
const int HotLEDPin = 20;          // Hot Button = GPIO 21 on Pico (Grove D20)

WiFiClient client;
HTTPClient http;
String authHeader;
String hostshort;

void setup(void) {

  int LEDPinOnboard = 32; 
  pinMode(LEDPinOnboard, OUTPUT);
  digitalWrite(LEDPinOnboard, HIGH);


  Wire.setSDA(8);
  Wire.setSCL(9);
  Wire.begin();
 
  if(!display.begin(SSD1306_SWITCHCAPVCC, 0x3C)) {
    Serial.println(F("SSD1306 allocation failed"));
    for(;;);
  }

  authHeader = hostName + AuthKey;
  hostshort = hostName; 
  hostshort.trim();
  Serial.println(authHeader);
  Serial.begin(115200);
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) 
  {
    delay(1000);
    Serial.println("Init >> Connecting to Wi-Fi...");
  }
  Serial.println("Init >> Wi-Fi connected successfully: " + WiFi.localIP().toString());
  pinMode(ColdButtonPin, INPUT);
  pinMode(HotButtonPin, INPUT);  
  pinMode(MainsWaterSelect, INPUT);  
  pinMode(ColdLEDPin, OUTPUT);
  pinMode(HotLEDPin, OUTPUT);  

}
void loop(void) {

  display.println("Hello, I'm Tappy Mc Tap Face!");
  delay(500);  

  JsonSendSensorData("C_" + hostshort, ! digitalRead(ColdButtonPin));
  JsonSendSensorData("H_" + hostshort, ! digitalRead(HotButtonPin));

  if (hostshort == "Bathroom") // Only the bathroom has the master override mains water connection switch :) 
  {
    JsonSendSensorData("MainsConnectedWater", digitalRead(MainsWaterSelect));
  }
  fetchRelayStatus();
  delay(250);
}

void JsonSendSensorData(String Sensor, bool value)
{
    std::string valueStr = std::to_string(value);
    String arduinoValueStr = String(valueStr.c_str());
    String rawUrl = serverAddress + "api/sensors/" + Sensor + "/" + arduinoValueStr;
    http.begin(rawUrl);
    http.addHeader("ServiceKey", authHeader);
    if (http.GET() == 200) 
    {
      Serial.print("HTTP Success -- Send: ");
    }
    else 
    {
      Serial.print("HTTP error -- Send: ");
    }
    Serial.print(Sensor);
    Serial.print(" | ");
    Serial.println(arduinoValueStr);
    http.end();
}
void fetchRelayStatus() {
 
  HTTPClient http;
  String url = serverAddress + "api/calculations";  // Convert hostName to arduino::String
  String authHeader = hostName + AuthKey;
  Serial.print("Connecting to url:");
  Serial.println(url);
  // Perform HTTP request
  http.begin(url);
  Serial.println(authHeader);
  http.addHeader("ServiceKey", authHeader);
  int httpResponseCode = http.GET();
  if (httpResponseCode == 200) 
  {
    JSONVar myObject = JSON.parse(http.getString());
    Serial.print("HTTP Success! code: ");
    Serial.println(httpResponseCode);
    Serial.println(myObject);
    http.end();

    
    bool ColdLEDState = LOW;
    bool HotLEDState = LOW;
    if (myObject["IsColdTapIn" + hostshort + "On"])
    {
      ColdLEDState = HIGH;
    }
    if (myObject["IsHotTapIn" + hostshort + "On"])
    {
      HotLEDState = HIGH;
    }
    digitalWrite(ColdLEDPin, ColdLEDState);
    digitalWrite(HotLEDPin, HotLEDState);

    display.clearDisplay();
    display.setTextSize(1);
    display.setTextColor(SSD1306_WHITE);
    display.setCursor(0,0);
    String place = hostshort + " Tappy:";
    
    String cold = myObject["IsColdTapIn" + hostshort + "On"] ? "ON" : "Off";
    //String colds = myObject["TimeHotTapIn" + hostshort + "On"];
    String hot = myObject["IsHotTapIn" + hostshort + "On"] ? "ON" : "Off";
    //String hots = myObject["TimeHotTapIn" + hostshort + "On"];
    String waste = myObject["WasteTankFull"] ? "YES" : "No";
    String clean = myObject["CleanTankEmpty"] ? "YES" : "No";
    String mains = myObject["OnMainsWater"] ? "Yes" : "NO";
    display.println(place);
    display.println("Connected to Bussy");
    display.print("Cold Tap: "); display.println(cold); //display.println(colds);
    display.print("Hot Tap: "); display.println(hot); //display.println(hots);
    display.print("Waste Full: "); display.println(waste); 
    display.print("Clean Empty: "); display.println(clean);
    display.print("On Mains Water: "); display.println(mains);
    display.display();
  } 
  else 
  {
    Serial.print("HTTP error code: ");
    Serial.println(httpResponseCode);
    http.end();
    display.clearDisplay();
    display.setTextSize(1);
    display.setTextColor(SSD1306_WHITE);
    display.setCursor(0,0);
    String place = hostshort + " Tappy:";
    
    display.println(place);
    display.println("Disconnected from Bussy");
    
    display.display();
  }
}