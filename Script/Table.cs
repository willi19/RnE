using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Table : MonoBehaviour
{
    public float min_eat_time;
    public float max_eat_time;
    public float min_empty_time;
    public float max_empty_time;
    public Material Waitfood;
    public Material Eating;
    public Material WaitReturnTray;
    public Material Empty;

    private Renderer rd;
    private int state = 0; //0 empty
                           //1 waiting food
                           //2 eating food
                           //3 waiting returning tray

    private float counter;

    public void init()
    {
        state = 0;
        counter = UnityEngine.Random.Range(min_empty_time, max_empty_time);
    }

    void Start()
    {
        rd = GetComponent<Renderer>();
        rd.material = Empty;
        init();
    }

    // Update is called once per frame
    void Update()
    {
        if(state == 0||state == 2)
        {
            counter -= Time.deltaTime;
            if (counter < 0)
                state++;
        }
        if (state == 0)
            rd.material = Empty;
        if (state == 1)
            rd.material = Waitfood;
        if (state == 2)
            rd.material = Eating;
        if (state == 3)
            rd.material = WaitReturnTray;
    }

    public void ReturnEmpty()
    {
        state = 0;
        counter = UnityEngine.Random.Range(min_empty_time, max_empty_time);
    }

    public void ReceiveFood()
    {
        state = 2;
        counter = UnityEngine.Random.Range(min_eat_time, max_eat_time);
    }

    public int GetState()
    {
        return state;
    }

    public bool IsWaiting()
    {
        return state == 1;
    }

    public bool IsReturnTray()
    {
        return state == 3;
    }
}
