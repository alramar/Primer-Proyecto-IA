using UnityEngine;
using System.Collections;

public class GhostController : MonoBehaviour
{
    public GameObject obj;
    [SerializeField] public GameObject player;
    [SerializeField] public GameObject vision;


    [SerializeField] private float posX1;
    [SerializeField] private float posZ1;
    [SerializeField] private float posX2;
    [SerializeField] private float posZ2;

    private float walkSpeed;
    [SerializeField] private float runSpeed;
    private float playerWalkRange;
    [SerializeField] private float playerRunRange;
    [SerializeField] private float vRange;

    private float rotateSpeed;

    private int behaviour;
    private int patrollState;
 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        obj = this.gameObject;
        walkSpeed = obj.GetComponent<Enemy>().speed;
        playerWalkRange = obj.GetComponent <Enemy>().detection_range;
        rotateSpeed = obj.GetComponent<Enemy>().rotation_speed;
        patrollState = 0;
        behaviour = 1; //0 = idle, 1 = patrulla, 2 = perseguir
    }

    // Update is called once per frame
    void Update()
    {
       if (player.GetComponent<PlayerMovement>().isRunning||player.GetComponent<PlayerMovement>().isWalking)
        {
            float distance = Vector3.Distance(obj.transform.position, player.transform.position);
            
            if(player.GetComponent<PlayerMovement>().isRunning && distance <= playerRunRange) { behaviour = 2; }
            if(player.GetComponent<PlayerMovement>().isWalking && distance <= playerWalkRange) { behaviour = 2; }
        }

        switch (behaviour)
        {
            case 0:break;
            case 1:
                if (transform.position == new Vector3(posX1, transform.position.y, posZ1)) 
                { patrollState = 0; transform.Rotate(new Vector3(0f, 0f, 180f)); }
                if (transform.position == new Vector3(posX2, transform.position.y, posZ2)) 
                { patrollState = 1; transform.Rotate(new Vector3(0f, 0f, 180f)); }
                patroll(); 
                break;
            default: chase(); break;
        }
    }

    void patroll()
    {
        float step = walkSpeed * Time.deltaTime;
        if (patrollState == 0) { transform.position = Vector3.MoveTowards(transform.position, new Vector3(posX2, transform.position.y, posZ2), step); }
        else { transform.position = Vector3.MoveTowards(transform.position, new Vector3(posX1, transform.position.y, posZ1), step); }
    }

    void chase()
    {
        transform.position = Vector3.MoveTowards(transform.position, player.transform.position, runSpeed * Time.deltaTime);
    }


}
