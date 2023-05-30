using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundCollider : MonoBehaviour
{
    private float rayLength;
    public LayerMask groundLayers;
    [HideInInspector] public bool grounded;
    private BoxCollider boxCol;
    private GameObject player;
    private bool rayFailure = false;
    private List<Vector3> positions;

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        boxCol = GetComponent<BoxCollider>();
        positions = new List<Vector3>();
        rayLength = boxCol.size.y / 2;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void LateUpdate()
    {
        //transform.position = player.transform.position;
    }

    private void FixedUpdate()
    {
        if (!grounded)
        {
            positions.Clear();
            positions.Add(new Vector3(player.transform.position.x + boxCol.size.x / 2, player.transform.position.y + boxCol.center.y, player.transform.position.z));
            positions.Add(new Vector3(player.transform.position.x - boxCol.size.x / 2, player.transform.position.y + boxCol.center.y, player.transform.position.z));
            positions.Add(new Vector3(player.transform.position.x, player.transform.position.y + boxCol.center.y, player.transform.position.z + boxCol.size.z / 2));
            positions.Add(new Vector3(player.transform.position.x, player.transform.position.y + boxCol.center.y, player.transform.position.z - boxCol.size.z / 2));
            for (int i = 0; i < 4; i++)
            {
                //Color rayColor = Color.red;
                if (Physics.Raycast(positions[i], -Vector3.up, rayLength, ~(1 << groundLayers)))
                {
                    grounded = true;
                    break;
                    // rayColor = Color.green;
                }
                else
                {
                    grounded = false;
                }
                //Debug.DrawRay(positions[i], -Vector3.up, Color.red, 0.1f);
            }
        }
       // Debug.DrawRay(boxCol.center + transform.position, -Vector3.up, rayColor, 0.1f);
       
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & groundLayers) != 0)
        {
            grounded = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        grounded = false;
    }
    
}
