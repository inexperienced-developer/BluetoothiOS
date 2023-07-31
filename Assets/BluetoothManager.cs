using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BluetoothManager : MonoBehaviour
{
    [SerializeField] private GameObject m_scannedItemPrefab;
    [SerializeField] private Transform m_scannedItemParent;

    public const string SPEED_AND_CADENCE_SERVICE_UUID = "00001816-0000-1000-8000-00805f9b34fb";
    public const string BATTERY_SERVICE_GUID = "0000180F-0000-1000-8000-00805F9B34FB";
    public const string BATTERY_LEVEL_CHARACTERISTIC_GUID = "00002A19-0000-1000-8000-00805F9B34FB";
    public const string CYCLING_SERVICE_GUID = "00001816-0000-1000-8000-00805f9b34fb";
    public const string CSC_CHARACTERISTIC_GUID = "00002A5B-0000-1000-8000-00805F9B34FB";

    private Dictionary<string, ScannedItem> m_scannedItems = new Dictionary<string, ScannedItem>();

    private void StartScan()
    {
        BluetoothLEHardwareInterface.ScanForPeripheralsWithServices(new string[] { SPEED_AND_CADENCE_SERVICE_UUID }, null, (address, name, rssi, bytes) => {
            BluetoothLEHardwareInterface.Log("item scanned: " + address);
            Debug.Log("item new: " + address);
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
}
