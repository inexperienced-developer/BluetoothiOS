using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class BluetoothManager : MonoBehaviour
{
    public static BluetoothManager Instance;

    [SerializeField] private GameObject m_scannedItemPrefab;
    [SerializeField] private Transform m_scannedItemParent;

    [SerializeField] private TMP_Text m_btnText;

    private event Action Connecting;
    
    public const string SPEED_AND_CADENCE_SERVICE_UUID = "00001816-0000-1000-8000-00805f9b34fb";
    public const string BATTERY_SERVICE_GUID = "0000180F-0000-1000-8000-00805F9B34FB";
    public const string BATTERY_LEVEL_CHARACTERISTIC_GUID = "00002A19-0000-1000-8000-00805F9B34FB";
    public const string CYCLING_SERVICE_GUID = "00001816-0000-1000-8000-00805f9b34fb";
    public const string CSC_CHARACTERISTIC_GUID = "00002A5B-0000-1000-8000-00805F9B34FB";

    public List<Service> ServicesToSubscribeTo { get; private set; } = new List<Service>();
    private Coroutine m_serviceRoutine;

    private Dictionary<string, ScannedItem> m_scannedItems = new Dictionary<string, ScannedItem>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Connecting += OnConnecting;
        BluetoothLEHardwareInterface.Initialize(true, false, () =>
        {
            m_btnText.SetText("Scan");
        }, (error) =>
        {

            m_btnText.SetText("Error during initialize: " + error);
        });
    }

    private void OnConnecting()
    {
        m_btnText.SetText("Connecting");
    }

    public void StartScan()
    {
        string[] scanUUID = new string[] { SPEED_AND_CADENCE_SERVICE_UUID };
        m_btnText.SetText("Scanning");
        BluetoothLEHardwareInterface.ScanForPeripheralsWithServices(scanUUID, null, (address, name, rssi, bytes) => {
            BluetoothLEHardwareInterface.Log("item scanned: " + address);
            Debug.Log("item new: " + address);
            m_btnText.SetText($"Found");
            if (m_scannedItems.ContainsKey(address))
            {
                var scannedItem = m_scannedItems[address];
                scannedItem.Init(name, address, rssi.ToString());
                BluetoothLEHardwareInterface.Log("already in list " + rssi.ToString());
            }
            else
            {
                BluetoothLEHardwareInterface.Log("item new: " + address);
                var newItem = Instantiate(m_scannedItemPrefab, m_scannedItemParent);
                if (newItem != null)
                {
                    BluetoothLEHardwareInterface.Log("item created: " + address);
                    var scannedItem = newItem.GetComponent<ScannedItem>();
                    //BluetoothManager.OnScannedItemAdded(scannedItem);

                    if (scannedItem != null)
                    {
                        BluetoothLEHardwareInterface.Log("item set: " + address);
                        scannedItem.Init(name, address, rssi.ToString());
                        //scannedItem.TextAddressValue.text = address;
                        //scannedItem.TextNameValue.text = name;
                        //scannedItem.TextRSSIValue.text = rssi.ToString();

                        m_scannedItems[address] = scannedItem;
                    }
                }
            }
        }, true);
    }

    public void Connect(string address)
    {
        BluetoothLEHardwareInterface.ConnectToPeripheral(address, (name) =>
        {
            m_btnText.SetText("Connecting");

        }, null, (address, serviceUUID, characteristicUUID) =>
        {
            //Characteristic Action
            //Get battery level characteristic
            bool batteryService = serviceUUID.ToUpper() == BluetoothManager.BATTERY_SERVICE_GUID.ToUpper() &&
                characteristicUUID.ToUpper() == BluetoothManager.BATTERY_LEVEL_CHARACTERISTIC_GUID.ToUpper();
            bool cadenceService = serviceUUID.ToUpper() == BluetoothManager.CYCLING_SERVICE_GUID.ToUpper() &&
                characteristicUUID.ToUpper() == BluetoothManager.CSC_CHARACTERISTIC_GUID.ToUpper();
            if (batteryService)
            {
                m_btnText.SetText("Connected Battery");
                SubscribeToPowerLevel(address);
                ServicesToSubscribeTo.Add(new BatteryService(m_btnText, address));
                if (m_serviceRoutine == null)
                {
                    m_serviceRoutine = StartCoroutine(NextService());
                }
                //BluetoothManager.OnConnectedItem(deviceName, address, serviceUUID);
            }
            else if (cadenceService)
            {
                m_btnText.SetText("Connected Cadence");
                SubscribeToCadence(address);
                ServicesToSubscribeTo.Add(new CadenceService(m_btnText, address));
                m_serviceRoutine = StartCoroutine(NextService());
                //BluetoothManager.OnConnectedItem(deviceName, address, serviceUUID);
            }
        });
    }

    private void SubscribeToPowerLevel(string deviceAddress)
    {
        //BluetoothLEHardwareInterface.ReadCharacteristic(deviceAddress, BluetoothManager.BATTERY_SERVICE_GUID, BluetoothManager.BATTERY_LEVEL_CHARACTERISTIC_GUID, (characteristic, bytes) =>
        //{
        //    ushort power = Convert.ToUInt16(bytes[0]);
        //    m_text.text += $"Power: {power}";
        //    BluetoothManager.OnPowerLevelChanged(power);
        //});
        BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress(deviceAddress, BluetoothManager.BATTERY_SERVICE_GUID, BluetoothManager.BATTERY_LEVEL_CHARACTERISTIC_GUID, (notifyAddress, notifyCharacteristic) =>
        {
            BluetoothLEHardwareInterface.ReadCharacteristic(deviceAddress, BluetoothManager.BATTERY_SERVICE_GUID, BluetoothManager.BATTERY_LEVEL_CHARACTERISTIC_GUID, (characteristic, bytes) =>
            {
                ushort power = Convert.ToUInt16(bytes[0]);
                //BluetoothManager.OnPowerLevelChanged(power);
            });
        }, null);
    }

    private IEnumerator NextService()
    {
        while (ServicesToSubscribeTo.Count > 0)
        {
            ServicesToSubscribeTo[0].Subscribe();
            yield return new WaitForSeconds(5);
            ServicesToSubscribeTo.RemoveAt(0);
        }
    }

    private void SubscribeToCadence(string deviceAddress)
    {
        BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress(deviceAddress, BluetoothManager.CYCLING_SERVICE_GUID, BluetoothManager.CSC_CHARACTERISTIC_GUID, (notifyAddress, notifyCharacteristic) =>
        {
            BluetoothLEHardwareInterface.ReadCharacteristic(deviceAddress, BluetoothManager.CYCLING_SERVICE_GUID, BluetoothManager.CSC_CHARACTERISTIC_GUID, (characteristic, bytes) =>
            {
                bool wheelRevolutionDataPresent = (bytes[0] & 0x01) != 0;
                m_btnText.text = $"Wheel Rev: {wheelRevolutionDataPresent}";
                if (wheelRevolutionDataPresent && bytes.Length >= 7)
                {
                    uint wheelRevolutions = bytes.ToUInt32(1);
                    m_btnText.text += $"Wheel Rev: {wheelRevolutions}";
                    // Last Wheel Event Time is another 16-bit integer in little-endian format starting at byte 5
                    ushort lastWheelEventTime = bytes.ToUInt16(5);
                    // Use wheel revolutions and last wheel event time
                    //BluetoothManager.OnWheelDataChanged(wheelRevolutions, lastWheelEventTime);
                }
                else if (bytes.Length >= 5)
                {
                    // Otherwise, try reading crank revolution data
                    ushort crankRevolutions = bytes.ToUInt16(1);
                    m_btnText.text += $"Crank Rev: {crankRevolutions}";
                    ushort lastCrankEventTime = bytes.ToUInt16(3);
                    // Use crank revolutions and last crank event time
                    //BluetoothManager.OnCrankDataChanged(crankRevolutions, lastCrankEventTime);
                }
            });
        }, null);
    }
}

public abstract class Service
{
    public TMP_Text Text;
    public string Address;

    public Service(TMP_Text text, string address)
    {
        Text = text;
        Address = address;
    }
    public abstract void Subscribe();


}

public class BatteryService : Service
{
    public BatteryService(TMP_Text text, string address) : base(text, address)
    {
    }

    public override void Subscribe()
    {
        SubscribeToPowerLevel(Address);
    }

    private void SubscribeToPowerLevel(string deviceAddress)
    {
        BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress(deviceAddress, BluetoothManager.BATTERY_SERVICE_GUID, BluetoothManager.BATTERY_LEVEL_CHARACTERISTIC_GUID, (notifyAddress, notifyCharacteristic) =>
        {
            BluetoothLEHardwareInterface.ReadCharacteristic(deviceAddress, BluetoothManager.BATTERY_SERVICE_GUID, BluetoothManager.BATTERY_LEVEL_CHARACTERISTIC_GUID, (characteristic, bytes) =>
            {
                ushort power = Convert.ToUInt16(bytes[0]);
                Text.text += $"Power: {power}";
                //BluetoothManager.OnPowerLevelChanged(power);
            });
        }, null);
    }
}

public class CadenceService : Service
{
    public CadenceService(TMP_Text text, string address) : base(text, address)
    {
    }

    public override void Subscribe()
    {
        SubscribeToCadence(Address);
    }

    private void SubscribeToCadence(string deviceAddress)
    {
        BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress(deviceAddress, BluetoothManager.CYCLING_SERVICE_GUID, BluetoothManager.CSC_CHARACTERISTIC_GUID, (notifyAddress, notifyCharacteristic) =>
        {
            BluetoothLEHardwareInterface.ReadCharacteristic(deviceAddress, BluetoothManager.CYCLING_SERVICE_GUID, BluetoothManager.CSC_CHARACTERISTIC_GUID, (characteristic, bytes) =>
            {
                Text.text = $"Byte Length: {bytes.Length}";
                bool wheelRevolutionDataPresent = (bytes[0] & 0x01) != 0;
                Text.text = $"Wheel Rev: {wheelRevolutionDataPresent}";
                if (wheelRevolutionDataPresent && bytes.Length >= 7)
                {
                    uint wheelRevolutions = bytes.ToUInt32(1);
                    Text.text += $"Wheel Rev: {wheelRevolutions}";
                    // Last Wheel Event Time is another 16-bit integer in little-endian format starting at byte 5
                    ushort lastWheelEventTime = bytes.ToUInt16(5);
                    // Use wheel revolutions and last wheel event time
                    //BluetoothManager.OnWheelDataChanged(wheelRevolutions, lastWheelEventTime);
                }
                else if (bytes.Length >= 5)
                {
                    // Otherwise, try reading crank revolution data
                    ushort crankRevolutions = bytes.ToUInt16(1);
                    Text.text += $"Crank Rev: {crankRevolutions}";
                    ushort lastCrankEventTime = bytes.ToUInt16(3);
                    // Use crank revolutions and last crank event time
                    //BluetoothManager.OnCrankDataChanged(crankRevolutions, lastCrankEventTime);
                }
            });
        }, null);
    }
}
