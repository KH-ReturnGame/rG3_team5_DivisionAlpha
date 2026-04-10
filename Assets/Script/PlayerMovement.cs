using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private bool _isMoving;
    private Vector3 _origPos, _targetPos;
    private float _timeToMove = 0.2f;
    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.wKey.isPressed && !_isMoving) StartCoroutine(MovePlayer(Vector3.up));
        if (Keyboard.current.aKey.isPressed && !_isMoving) StartCoroutine(MovePlayer(Vector3.left));
        if (Keyboard.current.sKey.isPressed && !_isMoving) StartCoroutine(MovePlayer(Vector3.down));
        if (Keyboard.current.dKey.isPressed && !_isMoving) StartCoroutine(MovePlayer(Vector3.right));
    }

    private IEnumerator MovePlayer(Vector3 direction)
    {
        _isMoving = true;
        float elapsedTime = 0;
        _origPos = transform.position;
        _targetPos = _origPos + direction;
        while (elapsedTime < _timeToMove)
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector3.Lerp(_origPos, _targetPos, elapsedTime / _timeToMove);
            yield return null;
        }
        transform.position = _targetPos;
        _isMoving = false;
    }
}
