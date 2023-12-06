using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UserControl : MonoBehaviour
{
    MainManager gm;
    AuthManager auth;
    const float speed = 5.0f;

    public Vector3 targetPos = Vector3.zero;
    public Vector3 rotation;
    Vector3 orgPos;
    public bool isRemote = false;

    public float rotationSpeed = 5.0f;
    public Transform cinemachineCamera;

    float scaleSize = 1;
    int currentHP;
    int maxHP;

    bool bMoving;

    void Start()
    {
        gm = GameObject.Find("GameManager").GetComponent<MainManager>();
        auth = GameObject.Find("AuthManager").GetComponent<AuthManager>();
    }

    private void OnEnable()
    {
        scaleSize = 1;
        transform.localScale = new Vector3(scaleSize, scaleSize, scaleSize);
        transform.position = new Vector3(Random.Range(-100, 100), 50, Random.Range(-100, 100));
    }

    void Update()
    {
        if (transform.position.y < -10)
            MainManager.Instance.OnLogOut();
        if (!isRemote)
        {
            targetPos.x = Input.GetAxisRaw("Horizontal");
            targetPos.z = Input.GetAxisRaw("Vertical");
            if (!EventSystem.current.IsPointerOverGameObject()
                && targetPos != new Vector3(0,0,0))
            {
                SendTargetPos();
            }
        }


        RotatePlayer();
        if (auth.EventText == "Jump" && Input.GetKey(KeyCode.Space))
            targetPos.y = 1;
        else
            targetPos.y = 0;

        if(auth.EventText == "Speed")
            transform.Translate(targetPos.normalized * speed * Time.deltaTime * 3);
        else
            transform.Translate(targetPos.normalized * speed * Time.deltaTime);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.collider.CompareTag("Slime"))
        {
            scaleSize += 0.1f;
            transform.localScale = new Vector3(scaleSize, scaleSize, scaleSize);
            Destroy(collision.gameObject);
        }
    }

    public void SendTargetPos()
    {
        gm.SendCommand("#Move#" + targetPos.x + ',' + targetPos.y);
    }

    public int GetHP()
    {
        return currentHP;
    }

    void RotatePlayer()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // 수평 회전 (플레이어 주위의 Y 축 기준)
        transform.Rotate(Vector3.up, -mouseX * rotationSpeed);

        // 수직 회전 (카메라 주위의 X 축 기준)
        cinemachineCamera.Rotate(Vector3.right, -mouseY * rotationSpeed);

        // 세로 회전을 제한하여 넘어가지 않도록 합니다.
        float xRotation = cinemachineCamera.localEulerAngles.x;
        xRotation = (xRotation > 180) ? xRotation - 360 : xRotation;

        // 세로 회전 제한 각도 설정 (시네머신 카메라에 따라 조절)
        float minRotation = -80.0f;
        float maxRotation = 80.0f;

        xRotation = Mathf.Clamp(xRotation, minRotation, maxRotation);

        cinemachineCamera.localEulerAngles = new Vector3(xRotation, 0, 0);
    }
}