using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using System.Security.Cryptography;


public class DeliverAgent : Agent
{

    public float moveSpeed = 5f;
    public float turnSpeed = 180f;
    public int size = 3;
    public float orderRadius = 20.0f;
    
    private int[] slot; //0 : null, 1 : food, 2: empty tray
    private int empty_cnt = 0;
    private int food_cnt = 0;
    private Restraunt restraunt;
    private Rigidbody rb;
    private Transform tr;
    private Table[] tables;
    private bool stopped;
    private GameObject returnTray, foodStall;

    //초기화 작업을 위해 한번 호출되는 메소드
    public override void Initialize()
    {
        tr = GetComponent<Transform>();
        rb = GetComponent<Rigidbody>();
        slot = new int[size];
        restraunt = GetComponentInParent<Restraunt>();
        tables = restraunt.tables;
        foodStall = restraunt.foodStall;
        returnTray = restraunt.returnTray;
    }

    public override void OnEpisodeBegin()
    {
        restraunt.ResetArea();
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        for (int i = 0; i < size; i++)
            slot[i] = 0;
        empty_cnt = 0;
        food_cnt = 0;
        stopped = true;
    }

    private void FixedUpdate()
    {
        for(int i=0;i<6;i++)
        {
            if (Vector3.Distance(tr.position, tables[i].transform.position) < orderRadius && stopped)
            {
                DeliverFood(tables[i]);
                GetEmptyTray(tables[i]);
            }
        }
        //Debug.Log(slot[0].ToString()+" "+slot[1].ToString()+" "+slot[2].ToString());
    }

    //환경 정보를 관측 및 수집해 정책 결정을 위해 브레인에 전달하는 메소드
    public override void CollectObservations(Unity.MLAgents.Sensors.VectorSensor sensor)
    {
        sensor.AddObservation(GetState());
        sensor.AddObservation(GetTableState());
        sensor.AddObservation(GetObjectState(foodStall));
        sensor.AddObservation(GetObjectState(returnTray));
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        // Convert the first action to forward movement
        float forwardAmount = vectorAction[0];

        // Convert the second action to turning left or right
        float turnAmount = 0f;
        if (vectorAction[1] == 1f)
        {
            turnAmount = -1f;
        }
        else if (vectorAction[1] == 2f)
        {
            turnAmount = 1f;
        }

        // Apply movement
        rb.MovePosition(tr.position + tr.forward * forwardAmount * moveSpeed * Time.fixedDeltaTime);
        tr.Rotate(tr.up * turnAmount * turnSpeed * Time.fixedDeltaTime);
        stopped = (turnAmount == 0 && forwardAmount == 0);
        // Apply a tiny negative reward every step to encourage action
        if (MaxStep > 0) AddReward(-1f / MaxStep);
    }

    //개발자(사용자)가 직접 명령을 내릴때 호출하는 메소드(주로 테스트용도 또는 모방학습에 사용)
    public override void Heuristic(float[] actionsOut)
    {
        float forwardAction = 0f;
        float turnAction = 0f;
        if (Input.GetKey(KeyCode.UpArrow))
            forwardAction = 1f;
        if (Input.GetKey(KeyCode.LeftArrow))
            turnAction = 1f;
        else if (Input.GetKey(KeyCode.RightArrow))
            turnAction = 2f;
        else if (Input.GetKey(KeyCode.DownArrow))
            forwardAction = -1f;
        actionsOut[0] = forwardAction;
        actionsOut[1] = turnAction;
        //Debug.Log($"[0]={actionsOut[0]} [1]={actionsOut[1]}");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.CompareTag("Table") || collision.transform.CompareTag("Wall"))
        {
            AddReward(-10.0f);
            EndEpisode();
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!stopped)
            return;
        if(collision.transform.CompareTag("FoodStall"))
        {
            int available = size - empty_cnt - food_cnt;
            if(available != 0)
            {
                int required_food = restraunt.WatingTable() - food_cnt;
                int getnum = Mathf.Min(available, required_food);
                GetFood(getnum);
                //Debug.Log("required amount :" + required_food.ToString() + "Get amount " + getnum.ToString()+ "Available" + available.ToString());
            }
        }
        if(collision.transform.CompareTag("TrayReturn"))
            ReturnTray();
    }

    private void GetFood(int getnum)
    {
        if (getnum <= 0)
            return;
        for (int i = 0; i < size; i++)
        {
            if (slot[i] == 0)
            {
                slot[i] = 1;
                getnum--;
                food_cnt++;
                AddReward(1f);
                if (getnum == 0)
                    break;
            }
        }
    }

    private void ReturnTray()
    {
        if (empty_cnt == 0)
            return;
        for (int i = 0; i < size; i++)
        {
            if (slot[i] == 2)
            {
                slot[i] = 0;
                AddReward(1f);
            }
        }
        empty_cnt = 0;
    }

    private void GetEmptyTray(Table table)
    {
        if(size == empty_cnt+food_cnt || !table.IsReturnTray())
            return;
        empty_cnt++;
        for (int i = 0; i < size; i++)
        {
            if (slot[i] == 0)
            {
                slot[i] = 2;
                AddReward(1f);
                break;
            }
        }
        table.ReturnEmpty();
    }

    private void DeliverFood(Table table)
    {
        if (food_cnt == 0 || !table.IsWaiting())
            return;
        for(int i=0;i<size;i++)
        {
            if(slot[i] == 1)
            {
                slot[i] = 0;
                AddReward(1f);
                break;
            }
        }
        food_cnt--;
        table.ReceiveFood();
    }

    private int State()
    {
        int c1 = 0, c2 = 0, c0 = 0;
        for(int i=0;i<slot.Length;i++)
        {
            if (slot[i] == 0)
                c0++;
            if (slot[i] == 1)
                c1++;
            if (slot[i] == 2)
                c2++;
        }
        if (c2 == 3)
            return 9;
        if (c2 == 2)
            return 8 - c0;
        if (c2 == 1)
            return 6 - c0;
        return 3 - c0;
    }

    private float[] GetState()
    {
        int ind = State();
        float[] ret = new float[10];
        for (int i = 0; i < 10; i++)
            ret[i] = 0.0f;
        ret[ind] = 1.0f;
        return ret;
    }

    private float[] GetTableState()
    {
        float[] ret = new float[42];
        for (int i = 0; i < 42; i++)
            ret[i] = 0.0f;
        for(int i=0;i<6;i++)
        {
            ret[6 * i + tables[i].GetState()] = 1.0f;
            float[] state = GetObjectState(tables[i].gameObject);
            for(int j = 0;j<state.Length;j++)
                ret[6 * i + 4 + j] = state[j];
        }
        return ret;
    }

    private float[] GetObjectState(GameObject obj)
    {
        float[] ret = new float[3];
        ret[0] = Vector3.Distance(obj.transform.position, tr.position);
        Vector3 tmp = (obj.transform.position - tr.position).normalized;
        ret[1] = tmp.x;
        ret[2] = tmp.z;
        return ret;
    }
}
