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
    private float timer;

    private Vector3 playerPosition;
 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        obj = this.gameObject;
        walkSpeed = obj.GetComponent<Enemy>().speed;
        playerWalkRange = obj.GetComponent <Enemy>().detection_range;
        rotateSpeed = obj.GetComponent<Enemy>().rotation_speed;
        patrollState = 0;
        behaviour = 1; //0 = idle, 1 = patrulla, 2 = sospecha, 3 = perseguir
        timer = 0;
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("tiempo: " + timer);
        Debug.Log("comportamiento: " + behaviour);
        if (behaviour < 2 && (player.GetComponent<PlayerMovement>().isRunning||player.GetComponent<PlayerMovement>().isWalking))
        {
            float distance = Vector3.Distance(obj.transform.position, player.transform.position);
            
            if(player.GetComponent<PlayerMovement>().isRunning && distance <= playerRunRange) { behaviour = 2; }
            if(player.GetComponent<PlayerMovement>().isWalking && distance <= playerWalkRange) { behaviour = 2; }
        }
        if (vision.GetComponent<ghostVision>().seePlayer == true) { behaviour = 3; }

        switch (behaviour)
        {
            case 0: 
                playerPosition = new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z);
                suspect();
                break;
            case 1:
                if (transform.position == new Vector3(posX1, transform.position.y, posZ1)) 
                { patrollState = 0; transform.Rotate(new Vector3(0f, 180f, 0f)); }
                if (transform.position == new Vector3(posX2, transform.position.y, posZ2)) 
                { patrollState = 1; transform.Rotate(new Vector3(0f, 180f, 0f)); }
                playerPosition = new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z);
                patroll(); 
                break;
            case 2:
                suspect(); break;
            default: chase(); break;
        }
    }
    Vector3 calculateRotation()
    {
        Vector3 playerGhostDiference = new Vector3(playerPosition.x - transform.position.x, transform.position.y, playerPosition.z - transform.position.z);
        Vector3 newDirection = Vector3.RotateTowards(transform.forward, playerGhostDiference, rotateSpeed * Time.deltaTime, 0f);
        return newDirection;
    }

    void patroll()
    {
        float step = walkSpeed * Time.deltaTime;
        if (patrollState == 0) { transform.position = Vector3.MoveTowards(transform.position, new Vector3(posX2, transform.position.y, posZ2), step); }
        else { transform.position = Vector3.MoveTowards(transform.position, new Vector3(posX1, transform.position.y, posZ1), step); }
    }

    void suspect()
    {
        if (transform.position != playerPosition)
        {
            timer = 0f;
            //Vector3 playerGhostDiference = new Vector3(playerPosition.x - transform.position.x, transform.position.y, playerPosition.z - transform.position.z);
            //Vector3 newDirection = Vector3.RotateTowards(transform.forward, playerGhostDiference, rotateSpeed * Time.deltaTime, 0f);
            transform.rotation = Quaternion.LookRotation(calculateRotation());
            //transform.LookAt(new Vector3(270f, playerPosition.y - transform.position.y, 0));
            transform.position = Vector3.MoveTowards(transform.position, playerPosition, runSpeed * Time.deltaTime);
        }
        else
        {
            if (timer < 3.0) { behaviour = 0; timer += Time.deltaTime; }
            else { behaviour = 1; timer = 0f; }
        }
    }

    void chase()
    {
        //Vector3 newDirection = Vector3.RotateTowards(transform.forward, playerPosition - transform.position, rotateSpeed * Time.deltaTime, 0f);
        //transform.rotation = Quaternion.LookRotation(newDirection);
        transform.rotation = Quaternion.LookRotation(calculateRotation());
        transform.position = Vector3.MoveTowards(transform.position, player.transform.position, runSpeed * Time.deltaTime);
    }



}
