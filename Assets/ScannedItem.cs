using TMPro;
using UnityEngine;

public class ScannedItem : MonoBehaviour
{
    [SerializeField] private TMP_Text m_name, m_address;
    public string Address { get; private set; }

    public void Init(string deviceName, string address, string rssi)
    {
        m_name.SetText($"{deviceName}");
        m_address.SetText($"{address}");
        Address = address;
    }
}
