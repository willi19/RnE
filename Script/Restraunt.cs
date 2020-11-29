using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using TMPro;

public class Restraunt : MonoBehaviour
{
    public DeliverAgent deliverAgent;

    public TextMeshPro cumulativeRewardText;

    public Table[] tables;

    public GameObject foodStall, returnTray;

    public static Vector3 ChooseStartPosition()
    {
        return new Vector3(-2, 1, 9);
    }
    
    private void PlaceDeliverAgent()
    {
        Rigidbody rigidbody = deliverAgent.GetComponent<Rigidbody>();
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        deliverAgent.transform.position = ChooseStartPosition();
        deliverAgent.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
    }

    private void ResetObjects()
    {
        for (int i = 0; i < 6; i++)
            tables[i].init();
    }

    public void ResetArea()
    {
        PlaceDeliverAgent();
        ResetObjects();
    }

    private void Start()
    {
        ResetArea();
    }

    private void Update()
    {
        // Update the cumulative reward text
        cumulativeRewardText.text = deliverAgent.GetCumulativeReward().ToString("0.00");
    }

    public int WatingTable()
    {
        int ret = 0;
        for(int i=0;i<tables.Length;i++)
        {
            if (tables[i].IsWaiting())
                ret++;
        }
        return ret;
    }
}
