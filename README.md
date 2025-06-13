# Dokumentacja projektu Line-Controller
## System monitorowania produkcji przemysłowej IoT

### Spis treści
1. [Wprowadzenie](#wprowadzenie)
2. [Instrukcja uruchomienia](#instrukcja-uruchomienia)
3. [Panel zarządzania](#panel-zarządzania)
4. [Komunikacja z platformą Azure](#komunikacja-z-platformą-azure)
5. [Logika biznesowa i kalkulacje](#logika-biznesowa-i-kalkulacje)
6. [Symulacja urządzeń](#symulacja-urządzeń)

### Wprowadzenie
Line-Controller to system monitorowania produkcji przemysłowej oparty na technologiach IoT, który umożliwia firmie X transformację procesów produkcyjnych poprzez połączenie linii produkcyjnych z platformą Azure IoT. System zapewnia monitorowanie w czasie rzeczywistym, analizę danych i automatyczną obsługę awarii.

#### Główne funkcjonalności:
- Monitorowanie wielu linii produkcyjnych w czasie rzeczywistym
- Automatyczna obsługa awarii i powiadomienia
- Analiza danych produkcyjnych i obliczanie KPI
- Zarządzanie parametrami produkcyjnymi
- Integracja z istniejącymi systemami OPC UA
- Dwukierunkowa komunikacja z urządzeniami

### Instrukcja uruchomienia

#### Wymagania systemowe:
- Windows 10/11
- Visual Studio 2022
- .NET 9.0 SDK
- Erlang/OTP (wymagany dla RabbitMQ)
- RabbitMQ Server
- Symulator urządzeń IIoTSim
- Konto Azure z dostępem do:
  - IoT Hub
  - Event Hub
  - Service Bus
  - Stream Analytics
  - Logic Apps
  - Storage Account

#### Krok po kroku:

1. **Przygotowanie środowiska:**
   - Zainstaluj Erlang/OTP
   - Zainstaluj RabbitMQ Server
   - Uruchom RabbitMQ w tle
   - Włącz interfejs webowy RabbitMQ (opcjonalnie):
     ```
     rabbitmq-plugins enable rabbitmq_management
     ```
   - Zainstaluj symulator IIoTSim

2. **Konfiguracja Azure:**
   a) IoT Hub:
      - Utwórz IoT Hub
      - Zarejestruj urządzenia, dla każdego urządzenia dodaj własność <br>"ProductionRate: [int]"
      - Skonfiguruj routing telemetrii do Event Hub
   
   b) Event Hub:
      - Utwórz Event Hub
      - Skonfiguruj połączenie z IoT Hub
   
   c) Service Bus:
      - Utwórz Service Bus
      - Utwórz kolejki:
        - emergency-queue (obsługa awarii)
        - email-queue (powiadomienia)
   
   d) Storage Account:
      - Utwórz konto magazynu
      - Utwórz tabele:
        - ErrorAlerts (alerty awaryjne)
        - TemperatureStats (statystyki temperatury)
        - ProductionKPIs (wskaźniki produkcji)
   
   e) Stream Analytics:
      - Utwórz Stream Analytics Job
      - Skonfiguruj wejście (Event Hub)
      - Skonfiguruj wyjścia (tabele i kolejki)
      - Przeklej zapytanie SQL z Code Snippets do odpowiedniego slotu w stream job, w razie potrzeby dostosuj nazwy wejść i wyjść
   
   f) Logic App:
      - Utwórz Logic App
      - Skonfiguruj trigger z kolejki email-queue (Service Bus)
        - Wykorzystaj w celu połączenia tożsamość zarządzaną Logic App
        - Pamiętaj by nadać uprawnienia Właściciela danych usługi Service Bus dla twojej tożsamości Logic App
      - Dodaj akcję wysyłania e-maili, pamiętaj o rozkodowaniu wiadomości z kolejki. Przykładowa treść maila znajduje się w Code Snippets

3. **Uruchomienie aplikacji:**
   a) Przygotowanie symulatora:
      - Uruchom IIoTSim
      - Utwórz nowe urządzenie
      - Zapamiętaj nazwę urządzenia (OpcUaName)
   
   b) Konfiguracja aplikacji:
      - Otwórz projekt w Visual Studio
      - Zaktualizuj plik config.json:
        - Ustaw OpcUaName zgodnie z symulatorem
        - Wprowadź poprawne connection stringi oraz nazwy urządzeń zgodnie z nazwami w IoT Hub
   
   c) Uruchomienie:
      - Wybierz profil "LaunchService"
      - Uruchom projekt
      - Sprawdź działanie konsol:
        - Emergency Alert Handler
        - Service Console
        - Device Logger


#### Struktura pliku konfiguracyjnego (config.json):
```
{
  "Devices": {
    "DefaultDevice": "Device1",
    "Device1": {
      "Name": "Device1", //nazwa wyświetlana w konsoli
      "OpcUaName": "Device 1", //nazwa urządzenia w symulatorze
      "OpcUaServerUrl": "opc.tcp://localhost:4840/",
      "IoTHubDeviceId": "Device_001", //nazwa urządzenia w IoT Hub
      "IoTHubConnectionString": "...",
      "OpcUaNodeIds": {
        "ProductionStatus": "ns=2;s={DeviceName}/ProductionStatus",
        "WorkorderId": "ns=2;s={DeviceName}/WorkorderId",
        "Temperature": "ns=2;s={DeviceName}/Temperature",
        "GoodCount": "ns=2;s={DeviceName}/GoodCount",
        "BadCount": "ns=2;s={DeviceName}/BadCount",
        "ProductionRate": "ns=2;s={DeviceName}/ProductionRate",
        "DeviceError": "ns=2;s={DeviceName}/DeviceError",
        "EmergencyStop": "ns=2;s={DeviceName}/EmergencyStop",
        "ResetErrorStatus": "ns=2;s={DeviceName}/ResetErrorStatus"
      }
    }
  }
}
```

#### Węzły OPC UA:
1. **Węzły telemetryczne (tylko odczyt):**
   - ProductionStatus (0 = zatrzymane, 1 = uruchomione) - kontrolowany wyłącznie przez symulator IIoTSim
   - WorkorderId (GUID aktualnego zlecenia)
   - GoodCount (liczba dobrych produktów)
   - BadCount (liczba wadliwych produktów)
   - Temperature (temperatura w °C)

2. **Węzły stanu:**
   - ProductionRate (odczyt/zapis, wartość w %)
   - DeviceError (odczyt/zapis, flagi błędów)

3. **Metody:**
   - EmergencyStop (zatrzymanie awaryjne)
   - ResetErrorStatus (resetowanie błędów)

### Panel zarządzania

#### Service Console
Panel główny oferuje następujące funkcjonalności:

1. **Devices connection panel:**
   - Lista wszystkich skonfigurowanych urządzeń
   - Status połączenia (połączone/rozłączone)
   - Możliwość nawiązania/zerwania połączenia
   - Filtrowanie urządzeń:
     - Wszystkie
     - Połączone
     - Rozłączone
    - Dla połączonych urządzeń pasywnie wysyłane są do chmury ich oczyty
      - ProductionStatus
      - WorkorderId
      - Temperature
      - GoodCount
      - BadCount
    - W razie wystąpienia błędu na urządzeniu wysyłany do chmury jest stosowny komunikat

2. **Active devices management:**
   - Zarządzanie połączonymi urządzeniami
   - Kontrola urządzenia za pomocą direct methods:
     - Wysyłanie konkretnej wiadomości na urządzenie
     - Uruchomienie serii wiadomości testowych z urządzenia na chmurę
     - Resetowanie błędów
     - Zatrzymanie awaryjne
   - Monitorowanie stanu urządzenia (tylko odczyt)
   - _**Production Rate zmieniać można jedynie poprzez zaktualizowanie bliźniaka ręcznie w chmurze.**_
   - _**Stan urządzenia (ProductionStatus) jest kontrolowany wyłącznie przez symulator IIoTSim**_

#### Device Logger
- Wyświetla komunikaty systemowe w czasie rzeczywistym
- Informacje o:
  - Połączeniach z urządzeniami
  - Wiadomościach telemetrycznych
  - Błędach i awariach
  - Zmianach stanu urządzeń
- Kolorowe oznaczenia komunikatów:
  - Czerwony: błędy i awarie
  - Standardowy: informacje ogólne

### Komunikacja z platformą Azure

#### Format wiadomości Device-to-Cloud (D2C):
```json
{
  "deviceId": "Device_001",
  "timestamp": "2024-03-20T10:30:00Z",
  "telemetry": {
    "productionStatus": 1,
    "workorderId": "550e8400-e29b-41d4-a716-446655440000",
    "temperature": 45.5,
    "goodCount": 100,
    "badCount": 2,
  }
}
```

#### Device Twin
1. **Właściwości desired:**
   - ProductionRate: Docelowa wartość produkcji (%)

2. **Właściwości reported:**
   - ProductionRate: Aktualna wartość produkcji
   - DeviceError: Stan błędów (flagi)

#### Metody bezpośrednie:
1. **EmergencyStop:**
   - Zatrzymuje urządzenie awaryjnie
   - Ustawia flagę Emergency Stop
   - Wysyła powiadomienie

2. **ClearErrors:**
   - Resetuje wszystkie flagi błędów
   - Przywraca normalny tryb pracy

3. **SendMessages**
    - Wysyła serię testowych wiadomości na chmurę



### Logika biznesowa i kalkulacje

#### Wskaźniki efektywności (KPI):
1. **Jakość produkcji:**
   - Procent dobrych produktów w całkowitej produkcji
   - Okno czasowe: 5 minut
   - Grupowanie po urządzeniu
   - Próg ostrzeżenia: < 90%

2. **Statystyki temperatury:**
   - Okno czasowe: 5 minut
   - Grupowanie po urządzeniu
   - Wskaźniki:
     - Średnia temperatura
     - Temperatura minimalna
     - Temperatura maksymalna
   - Aktualizacja co 1 minutę

3. **Monitorowanie błędów:**
   - Śledzenie liczby błędów w oknie 1 minuty
   - Próg awarii: > 3 błędów/minutę
   - Automatyczna reakcja:
     - Zatrzymanie awaryjne
     - Powiadomienie e-mail

#### Automatyczne reakcje:
1. **Wysoka liczba błędów:**
   - Wykrycie > 3 błędów w 1 minucie
   - Automatyczne zatrzymanie awaryjne
   - Wysłanie powiadomienia

2. **Niska jakość produkcji:**
   - Wykrycie < 90% dobrych produktów
   - Automatyczne zmniejszenie ProductionRate o 10%
   - Monitorowanie poprawy

3. **Wykrycie błędu:**
   - Dowolny typ błędu urządzenia
   - Wysłanie powiadomienia e-mail
   - Zapisanie alertu w bazie danych

### Kolejki Service Bus


1. **emergency-queue:**

     - Natychmiastowa obsługa awarii
     - Automatyczne zatrzymanie urządzenia
     - Mechanizm ponownych prób (3 próby)
   
2. **email-queue:**
     - Powiadomienia e-mail
     - Szczegóły awarii
     - Status urządzenia




### Symulacja urządzeń

#### IIoTSim Desktop:
1. **Instalacja:**
   - Pobierz IIoTSim.zip
   - Uruchom IIoTSim.Desktop.application
   - Postępuj zgodnie z instrukcją instalacji

2. **Konfiguracja urządzenia:**
   - Uruchom aplikację
   - Kliknij "New Device"
   - Zapamiętaj nazwę urządzenia
   - Gdy konfiguracja Line-Controller jest gotowa, a program działa, możesz uruchomić urządzenie.

3. **Ważne informacje:**
   - Serwer OPC UA: opc.tcp://localhost:4840/
   - Limit czasowy: 30 minut (wersja trial)
   - Wymagany restart po upływie limitu
   - Urządzenia istnieją tylko podczas działania aplikacji
   - Stan urządzenia (ProductionStatus) jest kontrolowany wyłącznie przez symulator
   - Aplikacja Line-Controller może tylko monitorować stan urządzenia, nie może go zmieniać

#### Testowanie:
1. **Połączenie OPC UA:**
   - Użyj przykładowego klienta OPC UA
   - URL: opc.tcp://localhost:4840/
   - Potwierdź połączenie
   - Sprawdź wartości węzłów

2. **Monitorowanie:**
   - Obserwuj dane w czasie rzeczywistym
   - Testuj różne scenariusze
   - Sprawdź reakcje systemu
   - Weryfikuj powiadomienia 
