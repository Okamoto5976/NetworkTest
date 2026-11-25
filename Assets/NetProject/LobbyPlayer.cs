using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayer : MonoBehaviour
{
    [SerializeField] private TMP_Text m_nameText;

    public event Action m_onReady;

    private void Awake()
    {
        //m_readyButton.onClick.AddListener(SetReady);
    }

    public void SetPlayerText(string name)
    {
        m_nameText.text = name;
    }

    public void SetReady()
    {
        //m_onReady?.Invoke();
    }
}
