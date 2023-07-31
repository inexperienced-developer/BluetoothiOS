using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScannedItem : MonoBehaviour
{
    [SerializeField] private TMP_Text m_name, m_address;
    public string Address { get; private set; }
    private Button m_btn;

    public void Init(string deviceName, string address, string rssi)
    {
        m_name.SetText($"{deviceName}");
        m_address.SetText($"{address}");
        Address = address;
        m_btn = GetComponent<Button>();
        SetupButton();
    }

    private void SetupButton()
    {
        m_btn.onClick.AddListener(() =>
        {
            Debug.Log("Connect");
            BluetoothManager.Instance.Connect(Address);
        });
    }
}
