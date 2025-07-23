# Line-Controller Project Documentation

## Industrial IoT Production Monitoring System

### Table of Contents

1. [Disclaimer](#disclaimer)
2. [Introduction](#introduction)
3. [Getting Started](#getting-started)
   - [System Requirements](#system-requirements)
   - [Step-by-step Guide](#step-by-step-guide)
   - [Configuration file structure (`config.json`)](#configuration-file-structure-configjson)
   - [OPC UA Nodes](#opc-ua-nodes)
4. [Device Simulation](#device-simulation)
5. [Management Panel](#management-panel)
   - [Devices Connection Panel](#1-devices-connection-panel)
   - [Active Devices Management](#2-active-devices-management)
   - [Device Logger](#device-logger)
6. [Azure Platform Communication](#azure-platform-communication)
   - [Device-to-Cloud (D2C) message format](#device-to-cloud-d2c-message-format)
   - [Device Twin](#device-twin)
   - [Direct methods](#direct-methods)
7. [Business Logic and Calculations](#business-logic-and-calculations)
   - [Key Performance Indicators (KPIs)](#key-performance-indicators-kpis)
   - [Automatic reactions](#automatic-reactions)
8. [Service Bus Queues](#service-bus-queues)
9. [Code Snippets](#code-snippets)

### Introduction

Line-Controller is an industrial production monitoring system based on IoT technologies, enabling company to transform production processes by connecting production lines to the Azure IoT platform. The system provides real-time monitoring, data analysis, and automatic failure handling.

#### Main Features:

- Real-time monitoring of multiple production lines
- Automatic failure handling and notifications
- Production data analysis and KPI calculation
- Management of production parameters
- Integration with existing OPC UA systems
- Bidirectional device communication

### Getting Started

#### System Requirements:

- Windows 10/11
- Visual Studio 2022
- .NET 9.0 SDK
- Erlang/OTP _(required for RabbitMQ)_
- RabbitMQ Server
- FFactorySim device simulator ([Source](https://github.com/dscountergo/FFactorySim))
- Azure account with access to:
  - IoT Hub
  - Service Bus
  - Stream Analytics
  - Logic Apps
  - Storage Account

#### Step-by-step Guide:

1.  **Environment Setup:**

    - Install **Erlang/OTP** and **RabbitMQ Server**.
    - Start the RabbitMQ service.
    - (Optional) Enable the RabbitMQ management interface for easier monitoring:
      ```
      rabbitmq-plugins enable rabbitmq_management
      ```
    - The **FFactorySim simulator** needs to be downloaded first. You can clone it from [GitHub](https://github.com/dscountergo/FFactorySim) or download as ZIP file.
    - After downloading, build and run the FFactorySim project to start the simulator.

2.  **Azure Resources Configuration:**
    _This guide assumes you are familiar with creating resources in the Azure Portal._

    a) **IoT Hub:**

    - Create an IoT Hub instance.
    - Register your devices. For each device, add a device twin tag named `ProductionRate` with an integer value (e.g., `100`). This is used to control the device's production speed.

    b) **Service Bus:**

    - Create a Service Bus namespace.
    - Create two queues:
      - `emergency-queue`: For high-priority alerts that trigger immediate actions.
      - `email-queue`: For sending email notifications about device events.

    c) **Storage Account:**

    - Create a Storage Account.
    - In the Table service, create three tables:
      - `ErrorAlerts`: To log all critical device failure alerts.
      - `TemperatureStats`: To store aggregated temperature statistics (avg, min, max) for each device.
      - `ProductionKPIs`: To store calculated production metrics like quality percentage and efficiency.

    d) **Stream Analytics:**

    - Create a Stream Analytics Job.
    - Configure the **input** to be your IoT Hub (select your IoT Hub as the data source).
    - Configure **outputs** to be your Service Bus queues and Storage Account tables.
    - Paste the SQL query from [StreamJob.txt](#1-stream-analytics-job-streamjobtxt) into the query editor.
    - **Important:** Make sure that the input and output names in the SQL query match the names you configured in your Stream Analytics Job.
    - **Save** the Stream Analytics job.

    e) **Logic App:**

    - Create a Logic App.
    - Set the trigger to be a new message in the `email-queue` (Service Bus).
      - Use a managed identity for the Logic App for secure access.
      - Grant this identity the "Azure Service Bus Data Receiver" role on your Service Bus.
    - Add an action to send an email. Use the template from [LogicApp.txt](#2-logic-app-email-template-logicapptxt) for the email body.

3.  **Local Application Setup & Launch:**

    a) **Configure the Project:**

    - Open the `Line-Controller.sln` solution in Visual Studio.
    - In Visual Studio, select the `LaunchService` profile and run the project for the first time. This will automatically generate the `Shared/config.json` file if it does not exist.
    - Open the generated `Shared/config.json` file and update it with the correct connection strings for your Azure resources and the device details (`IoTHubDeviceId`, `OpcUaName`, etc.).

    b) **Run the System:**

    - Start the **FFactorySim simulator** and create/start a device. Note its `OpcUaName` listed under "Devices".
    - Ensure your `config.json` file uses the correct `OpcUaName`.
    - In Visual Studio, select the "LaunchService" profile and run the project.
    - The following console windows should appear and start processing data:
      - `Emergency Alert Handler`
      - `Service Console`
      - `Device Logger`

#### Configuration file structure (`config.json`)

> **Tip:**  
> On the first run, the application will automatically generate a `Shared/config.json` file (if it does not exist), based on the example file.  
> **After the first run, open `Shared/config.json` and fill in your own values as described below.**  
> You can also use `Shared/config.example.json` as a template.

```json
{
  "Devices": {
    "DefaultDevice": "Device1", // Name of the default device to use
    "Device1": {
      "Name": "<DEVICE_NAME>", // Display name for the device
      "OpcUaName": "<LOCAL_SIMULATOR_DEVICE_NAME>", // Name as configured in FFactorySim
      "OpcUaServerUrl": "opc.tcp://localhost:4840/", // OPC UA server address
      "IoTHubDeviceId": "<IOT_HUB_DEVICE_CLOUD_NAME>", // Device ID in Azure IoT Hub
      "IoTHubConnectionString": "<YOUR_DEVICE_CONNECTION_STRING>", // Device connection string from IoT Hub
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
    //you can add here another device
  },
  "ServiceController": {
    "ConnectionString": "<YOUR_SERVICE_CONTROLLER_CONNECTION_STRING>" // Connection string for Service Controller
  },
  "ServiceBus": {
    "ConnectionString": "<YOUR_SERVICE_BUS_CONNECTION_STRING>", // Azure Service Bus connection string
    "QueueName": "emergency-queue" // Name of the queue for emergency alerts
  }
}
```

#### OPC UA Nodes:

1. **Telemetry nodes (read-only):**

   - ProductionStatus (0 = stopped, 1 = running)
   - WorkorderId (current order GUID)
   - GoodCount (number of good products)
   - BadCount (number of defective products)
   - Temperature (temperature in °C)

2. **State nodes:**

   - ProductionRate (read/write, value in %)
   - DeviceError (read/write, error flags)

3. **Methods:**
   - EmergencyStop (emergency stop)
   - ResetErrorStatus (reset errors)

### Management Panel

The management console provides two main panels for device management:

#### 1. Devices Connection Panel

- **View all configured devices** with their connection status (Online/Offline).
- **Filter devices** by status: All, Connected, or Disconnected.
- **Connect/Disconnect devices**:
  - Start a device to begin sending telemetry to the cloud.
  - Stop a device to disconnect it from the system.
- **Status display**:
  - Each device is listed with its current status.
- **Automatic telemetry**:
  - When a device is connected, it passively sends telemetry (ProductionStatus, WorkorderId, Temperature, GoodCount, BadCount) to Azure IoT Hub.
- **Error handling**:
  - If a device encounters an error, an appropriate alert is sent to the cloud.

#### 2. Active Devices Management

- **Manage connected devices**:
  - Select a connected device to perform further actions.
- **Available actions for each device:**
  - **Send Cloud-to-Device (C2D) Message**: Send a custom message to the device.
  - **Execute Direct Method**:
    - `SendMessages`: Trigger the device to send a series of test messages to the cloud.
    - `EmergencyStop`: Remotely trigger an emergency stop (with confirmation).
    - `ClearErrors`: Reset all error flags on the device.
  - **Update Device Twin**:
    - Add or update any property in the device twin (property name and random value).
    - _Note: Production Rate can only be changed by manually updating the twin property in the cloud._
- **Device state (ProductionStatus)** is controlled exclusively by the FFactorySim simulator and cannot be changed from the management console.

#### Device Logger

The Device Logger is a real-time console tool for monitoring all device events and messages in the system.

- **Displays logs from all devices** in real time, with each message prefixed by the device name and timestamp.
- **Color-codes messages** for better readability:
  - Device name is always shown in yellow.
  - Messages containing the keyword **error** are shown in red.
  - All other messages are shown in blue (default).
- **Types of messages displayed:**
  - Device connections and disconnections
  - Telemetry data (e.g., temperature, production counts)
  - Errors and failures
  - State changes and alerts
  - Any other system or device events published to the log exchange
- **Log source:**
  - All logs are received via RabbitMQ from the `device_logs` exchange.
  - Any component in the system can publish messages to be displayed in the logger.
- **Usage:**
  - Start the Device Logger to monitor the system.
  - The logger will display all incoming messages until you press any key to exit.

### Azure Platform Communication

#### Device-to-Cloud (D2C) message format:

```json
{
  "deviceId": "Device_001",
  "timestamp": "2024-03-20T10:30:00Z",
  "telemetry": {
    "productionStatus": 1,
    "workorderId": "550e8400-e29b-41d4-a716-446655440000",
    "temperature": 45.5,
    "goodCount": 100,
    "badCount": 2
  }
}
```

#### Device Twin

1. **Desired properties:**

   - ProductionRate: Target production value (%)

2. **Reported properties:**
   - ProductionRate: Current production value
   - DeviceError: Error state (flags)

#### Direct methods:

1. **EmergencyStop:**

   - Emergency stop of the device
   - Sets the Emergency Stop flag
   - Sends a notification via e-mail

2. **ClearErrors:**

   - Resets all error flags
   - Restores normal operation

3. **SendMessages**
   - Sends a series of test messages to the cloud

### Business Logic and Calculations

#### Key Performance Indicators (KPIs):

1. **Production quality:**

   - Percentage of good products in total production
   - Time window: 5 minutes
   - Grouped by device
   - Warning threshold: < 90%

2. **Temperature statistics:**

   - Time window: 5 minutes
   - Grouped by device
   - Indicators:
     - Average temperature
     - Minimum temperature
     - Maximum temperature
   - Updated every 1 minute

3. **Error monitoring:**
   - Tracking the number of errors in a 1-minute window
   - Failure threshold: 3 or more errors/minute
   - Automatic reaction:
     - Emergency stop
     - Email notification

#### Automatic reactions:

1. **High number of errors:**

   - Detecting 3 or more errors in 1 minute
   - Automatic emergency stop
   - Sending notification

2. **Low production quality:**

   - Detecting < 90% good products
   - Automatically decrease ProductionRate by 10%
   - Monitor improvement

3. **Error detected:**
   - Any type of device error
   - Send email notification
   - Save alert to database

### Service Bus Queues

1. **emergency-queue:**

   - Immediate failure handling
   - Automatic device stop
   - Retry mechanism (3 attempts)

2. **email-queue:**
   - Email notifications
   - Failure details
   - Device status

### Device Simulation

The project uses the **FFactorySim** simulator (available from [GitHub](https://github.com/dscountergo/FFactorySim)) to emulate industrial IoT devices and generate realistic production data.

#### How it works

- Each simulated device exposes an OPC UA server with a set of telemetry and state nodes, as described in the [OPC UA Nodes](#opc-ua-nodes) section.
- The simulator allows you to create, start, and manage virtual devices, which then interact with the Line-Controller system just like real hardware.

#### Installation & Usage

1. **Start the Simulator**

   - Download FFactorySim
   - Build and run the FFactorySim project to launch the simulator.

2. **Create and Configure Devices**

   - In the FFactorySim application, click **"Add Device"** to add a virtual device.
   - Remember the device name (`OpcUaName`) – you will need it in your `config.json`.
   - You can create multiple devices if needed.

3. **Connect to the Line-Controller**

   - Make sure your `config.json` contains the correct `OpcUaName` and OPC UA server address (`opc.tcp://localhost:4840/`) for each device.
   - Start the Line-Controller application (see [Getting Started](#getting-started)).

4. **Simulation Details**

   - Each device simulates production data: status, workorder, good/bad counts, temperature, errors, etc.
   - Device state (`ProductionStatus`) is controlled only from the simulator UI.
   - The Line-Controller system can only monitor device state and send direct method commands (e.g., EmergencyStop, ResetErrorStatus).

5. **Limitations**
   - The trial version of IIoTSim may have a time limit (e.g., 30 minutes) and require a restart.
   - Devices exist only while the simulator is running.

#### Testing & Monitoring

- You can use any OPC UA client to connect to the simulator at `opc.tcp://localhost:4840/` and browse node values.
- All telemetry and state changes will be reflected in the Line-Controller system and visible in the management console and logger.

## Code Snippets

This repository includes ready-to-use code snippets for quick configuration of Azure resources:

### 1. Stream Analytics Job (StreamJob.txt)

- **Location:** `CodeSnippets/StreamJob.txt`
- **Description:**
  Contains complete SQL queries for Azure Stream Analytics to:
  - Archive production KPIs (quality, good/bad count, efficiency)
  - Archive temperature statistics (average, min, max)
  - Archive device error alerts
  - Route alerts to Service Bus queues and email notifications
- **How to use:**
  Copy the relevant SQL queries into your Stream Analytics job configuration in the Azure portal. Adjust input/output names if needed.

### 2. Logic App Email Template (LogicApp.txt)

- **Location:** `CodeSnippets/LogicApp.txt`
- **Description:**
  Contains a ready-to-use JSON fragment for the subject and body of the email action in Azure Logic App.
  The template dynamically fills in device data and error details.
- **Important:** The template includes `base64ToString()` function calls because Service Bus messages are Base64 encoded. This decoding is necessary for proper data extraction.
- **How to use:**
  In your Logic App, switch to code view in the email action and paste the provided subject and body content.
