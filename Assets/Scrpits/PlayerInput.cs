
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    // 다른 스크립트가 읽어갈 수 있도록 public으로 공개
    public Joystick joystick; // 조이스틱 컴포넌트 참조
    public Vector3 joystickInput { get; private set; }

    public Vector3 MoveInput { get; private set; }
    public Vector3 LookInput { get; private set; }
    public bool JumpPressed { get; private set; }

    // Input Action Asset에서 만든 액션을 이 함수들이 자동으로 호출해줌
    // (PlayerInput 컴포넌트의 Behavior를 "Send Messages"로 설정)
    public void Update()
    {
        joystickInput = new Vector3(
            joystick.Horizontal,
            0f,
            joystick.Vertical
        );
    }

    public void OnMove(InputValue value)
    {
        MoveInput = value.Get<Vector3>();
    }

    // public void OnLook(InputValue value)
    // {
    //     LookInput = value.Get<Vector3>();
    // }

    // public void OnJump(InputValue value)
    // {
    //     JumpPressed = value.isPressed;
    // }
}
    

