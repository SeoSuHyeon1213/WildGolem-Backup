using UnityEngine;

public class PlayTouch : MonoBehaviour
{
    private Rigidbody playerRigidbody;
    public float speed = 10f;
    public float rotationSpeed = 10f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        Invoke("Update", 3);
    }

    // Update is called once per frame
    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical"); //축 값 지정(키보드에서 입력받는 값)

        Vector3 moveDirection = new Vector3(x, 0f, z);//(키보드에서 입력받는 값으로 이동 방향 벡터 생성)
        if (moveDirection.sqrMagnitude > 0.1f) //생성된 벡터 방향대로 회전
        {
            Quaternion forwardRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, forwardRotation, rotationSpeed * Time.deltaTime);
        }


        float xSpeed = x * speed; //(키보드에서 입력 받은 값에 속도 값을 곱하여 이동 속도 계산)
        float zSpeed = z * speed;

        Vector3 newVelocity = new Vector3(xSpeed, 0f, zSpeed);

        playerRigidbody.linearVelocity = newVelocity;
    }
}
