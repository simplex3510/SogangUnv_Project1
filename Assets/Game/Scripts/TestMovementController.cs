using UnityEngine;
using UnityEngine.InputSystem;

public class TestMovementController : MonoBehaviour
{
    [SerializeField] private InputActionReference _moveInput;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private float _moveSpeed;

    private Vector2 _moveInputVector;

    private void FixedUpdate()
    {
        // 2. 방향 벡터를 계산합니다.
        Vector3 moveDirection = (transform.right * _moveInputVector.x + transform.forward * _moveInputVector.y).normalized;
        moveDirection.y = 0f;

        // 3. 기존 Y축 속도(중력)는 유지하면서 X, Z축 속도만 변경합니다.
        Vector3 targetVelocity = moveDirection * _moveSpeed;
        targetVelocity.y = _rigidbody.linearVelocity.y;

        _rigidbody.linearVelocity = targetVelocity;
    }

    private void Update()
    {
        // 1. 입력은 매 프레임(Update) 정확하게 읽어옵니다.
        _moveInputVector = _moveInput.action.ReadValue<Vector2>();
    }
}
