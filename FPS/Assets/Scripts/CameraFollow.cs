using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Range(0, 1)]
    [SerializeField] float speed = 0.1f;

    private FirstPersonMovement fpsm;
    private GameObject player;

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        fpsm = player.GetComponent<FirstPersonMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, fpsm.cameraPosTarget, speed);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(-fpsm.mousePosY,
            player.transform.rotation.eulerAngles.y, 0), speed);
    }
    private void FixedUpdate()
    {

    }

    private void LateUpdate()
    {


    }
}
