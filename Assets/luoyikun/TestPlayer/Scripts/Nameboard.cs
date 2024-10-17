using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Nameboard : MonoBehaviour
{
    public RichText m_actorName;
    public RichImage m_hp;
    public RichTextRender m_richRender;

    public void SetName(string name)
    {
        m_actorName.text = name;
    }

    public void SetHp(float hp)
    {
        m_hp.fillAmount = hp;
    }

    public void SetIsMajor(bool isMajor)
    {
        m_richRender.m_isMajor = isMajor;
    }
}
