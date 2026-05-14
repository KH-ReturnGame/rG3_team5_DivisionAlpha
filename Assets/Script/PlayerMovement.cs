using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private bool _isMoving;
    private Vector3 _currentDirection;
    private Vector3 _lastMoveDirection;

    [SerializeField]
    private float _timeToMove = 0.2f;

    void Update()
    {
        HandleInput();

        if (!_isMoving && _currentDirection != Vector3.zero) StartCoroutine(MovePlayer(_currentDirection));
    }

    private void HandleInput()
    {
        Vector3 input = Vector3.zero;

        // 키 홀드 감지
        if (Keyboard.current.wKey.isPressed) input = Vector3.up;
        else if (Keyboard.current.sKey.isPressed) input = Vector3.down;
        else if (Keyboard.current.aKey.isPressed) input = Vector3.left;
        else if (Keyboard.current.dKey.isPressed) input = Vector3.right;

        // 입력이 없으면 방향 초기화
        if (input == Vector3.zero)
        {
            _currentDirection = Vector3.zero;
            return;
        }

        // 현재 이동 방향과 같으면 버퍼 추가 안함
        if (input != _lastMoveDirection || !_isMoving) _currentDirection = input;
    }

    private IEnumerator MovePlayer(Vector3 direction)
    {
        _isMoving = true;
        _lastMoveDirection = direction;

        float elapsedTime = 0f;

        Vector3 originPos = transform.position;
        Vector3 targetPos = originPos + direction;

        while (elapsedTime < _timeToMove)
        {
            elapsedTime += Time.deltaTime;

            transform.position = Vector3.Lerp(
                originPos,
                targetPos,
                elapsedTime / _timeToMove
            );

            yield return null;
        }

        transform.position = targetPos;

        _isMoving = false;

        // 키를 계속 누르고 있으면 자동 연속 이동
        if (_currentDirection != Vector3.zero) StartCoroutine(MovePlayer(_currentDirection));
    }
}