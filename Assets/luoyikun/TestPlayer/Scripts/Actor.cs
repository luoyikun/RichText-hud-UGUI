using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Actor : MonoBehaviour
{
    static int s_id = 0; //测试用，让id自增，不用手动填入面板中
    public int m_id;
    public Nameboard m_nameboard;
    private void Start()
    {
        m_id = s_id;
        s_id++;
    }
    private void Awake()
    {
        NameboardMgr.Instance.Init();
    }

    private void OnEnable()
    {
        //名字板创建
        if (m_nameboard == null)
        {
            GameObject menObj = Resources.Load("Nameboard") as GameObject;
            GameObject insObj = Instantiate(menObj);
            m_nameboard = insObj.GetComponent<Nameboard>();
        }

        if (m_id == 0)
        {
            m_nameboard.SetIsMajor(true);
        }
        else
        {
            m_nameboard.SetIsMajor(false);
        }
        m_nameboard.gameObject.SetActive(true);
        NameboardMgr.Instance.AddNameboard(this, this.m_nameboard);
    }

    private void OnDisable()
    {
        m_nameboard.gameObject.SetActive(false);
        NameboardMgr.Instance.RemoveNameboard(this, this.m_nameboard);
    }
}
