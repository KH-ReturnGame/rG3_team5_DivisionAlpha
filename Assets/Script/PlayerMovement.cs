using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private bool _isMoving;
    private Vector3 _origPos, _targetPos;
    private float _timeToMove = 0.13f;

    [Header("인간 방향별 스프라이트 등록")]
    [SerializeField] private Sprite spriteUp;    
    [SerializeField] private Sprite spriteDown;  
    [SerializeField] private Sprite spriteLeft;  
    [SerializeField] private Sprite spriteRight; 

    private SpriteRenderer _spriteRenderer; 

    [Header("1스킬: 휘두르기 (J키)")]
    [SerializeField] private GameObject swingEffectObject; 
    [SerializeField] private Image swingBorderImage;   // 1스킬 빨간 테두리 Image
    [SerializeField] private Image swingCoolDownImage;  // 1스킬 안쪽 흰색 원형 게이지 Image
    
    [HideInInspector] private float swingDuration = 0.25f; // 애니메이션 0.25초
    [HideInInspector] private float swingCooldown = 0.75f; // 쿨타임 0.75초
    private float _swingCooldownTimer = 0f;

    [Header("2스킬: 카운터 (K키)")]
    [SerializeField] private GameObject counterEffectObject; 
    [SerializeField] private Image counterBorderImage;  // 2스킬 빨간 테두리 Image
    [SerializeField] private Image counterCoolDownImage; // 2스킬 안쪽 흰색 원형 게이지 Image
    [SerializeField] private float counterDuration = 0.3f;
    [SerializeField] private float counterCooldown = 7.0f; 
    private float _counterCooldownTimer = 0f;

    private Vector3 _lookDirection = Vector3.down; 
    private Coroutine _attackEffectCoroutine;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (swingEffectObject != null) swingEffectObject.SetActive(false);
        if (counterEffectObject != null) counterEffectObject.SetActive(false);

        // [초기화] 게임 시작할 때 UI들의 기본 상태를 강제로 세팅
        ResetUIState(swingBorderImage, swingCoolDownImage);
        ResetUIState(counterBorderImage, counterCoolDownImage);

        UpdateGridSortingOrder();
    }

    void Update()
    {
        // 쿨타임 타이머가 작동 중일 때만 UI를 갱신하도록 변경 (매 프레임 덮어쓰기 방지)
        if (_swingCooldownTimer > 0)
        {
            UpdateCooldownUI(ref _swingCooldownTimer, swingCooldown, swingBorderImage, swingCoolDownImage);
        }
        if (_counterCooldownTimer > 0)
        {
            UpdateCooldownUI(ref _counterCooldownTimer, counterCooldown, counterBorderImage, counterCoolDownImage);
        }

        // 1. 이동 처리
        if (Keyboard.current.wKey.isPressed && !_isMoving) 
        {
            _lookDirection = Vector3.up;
            ChangePlayerSprite(Vector3.up);
            StartCoroutine(MovePlayer(Vector3.up));
        }
        else if (Keyboard.current.aKey.isPressed && !_isMoving) 
        {
            _lookDirection = Vector3.left;
            ChangePlayerSprite(Vector3.left);
            StartCoroutine(MovePlayer(Vector3.left));
        }
        else if (Keyboard.current.sKey.isPressed && !_isMoving) 
        {
            _lookDirection = Vector3.down;
            ChangePlayerSprite(Vector3.down);
            StartCoroutine(MovePlayer(Vector3.down));
        }
        else if (Keyboard.current.dKey.isPressed && !_isMoving) 
        {
            _lookDirection = Vector3.right;
            ChangePlayerSprite(Vector3.right);
            StartCoroutine(MovePlayer(Vector3.right));
        }

        // 2. 1스킬 휘두르기 (J키)
        if (Keyboard.current.jKey.wasPressedThisFrame && _swingCooldownTimer <= 0)
        {
            TrySwingAttack();
        }

        // 3. 2스킬 카운터 (K키)
        if (Keyboard.current.kKey.wasPressedThisFrame && _counterCooldownTimer <= 0)
        {
            TryCounterAttack();
        }
    }

    // ★ UI 상태를 안전하게 제어하는 핵심 함수
    private void UpdateCooldownUI(ref float timer, float maxCooldown, Image borderImg, Image coolDownImg)
    {
        timer -= Time.deltaTime;

        if (timer > 0)
        {
            float fillRatio = (maxCooldown - timer) / maxCooldown;

            // 1. 빨간 테두리: 시계방향 차오름 연산
            if (borderImg != null && borderImg.type == Image.Type.Filled)
            {
                borderImg.fillAmount = fillRatio;
            }

            // 2. 안쪽 흰색 원: 투명도 25%로 낮춤
            if (coolDownImg != null)
            {
                Color cColor = coolDownImg.color;
                cColor.a = 0.25f; 
                coolDownImg.color = cColor;
                coolDownImg.fillAmount = 1f; 
            }
        }
        else
        {
            // 3. 쿨타임이 딱 끝난 순간 복구
            timer = 0f;
            ResetUIState(borderImg, coolDownImg);
        }
    }

    // 평소 대기 상태로 UI를 되돌리는 깔끔한 분리 함수
    private void ResetUIState(Image borderImg, Image coolDownImg)
    {
        if (borderImg != null)
        {
            borderImg.fillAmount = 1f; // 테두리 100% 장착
        }
        if (coolDownImg != null)
        {
            Color cColor = coolDownImg.color;
            cColor.a = 0.6f; // 안쪽 원 원래 선명도(60%) 복구
            coolDownImg.color = cColor;
            coolDownImg.fillAmount = 1f;
        }
    }

    private void ChangePlayerSprite(Vector3 direction)
    {
        if (_spriteRenderer == null) return;
        if (direction == Vector3.up && spriteUp != null) _spriteRenderer.sprite = spriteUp;
        else if (direction == Vector3.down && spriteDown != null) _spriteRenderer.sprite = spriteDown;
        else if (direction == Vector3.left && spriteLeft != null) _spriteRenderer.sprite = spriteLeft;
        else if (direction == Vector3.right && spriteRight != null) _spriteRenderer.sprite = spriteRight;
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
        
        UpdateGridSortingOrder();

        _isMoving = false;
    }

    private void TrySwingAttack()
    {
        Debug.Log("인간 1스킬 일반 휘두르기 공격 (J키)!");
        _swingCooldownTimer = swingCooldown; // 타이머 작동 시작!
        
        // 스킬을 누른 '첫 프레임'에 테두리를 0으로 깎고 연출 시작하도록 강제 설정
        if (swingBorderImage != null) swingBorderImage.fillAmount = 0f;

        if (_attackEffectCoroutine != null) StopCoroutine(_attackEffectCoroutine);
        _attackEffectCoroutine = StartCoroutine(InstantSwingRotationRoutine());

        int myX = Mathf.RoundToInt(transform.position.x);
        int myY = Mathf.RoundToInt(transform.position.y);
        List<Vector2Int> targetTiles = GetFrontThreeTiles(myX, myY);

        ZombieAIWithCounter[] zombies = Object.FindObjectsByType<ZombieAIWithCounter>(FindObjectsSortMode.None);
        foreach (ZombieAIWithCounter zombie in zombies)
        {
            int zombieX = Mathf.RoundToInt(zombie.transform.position.x);
            int zombieY = Mathf.RoundToInt(zombie.transform.position.y);

            foreach (Vector2Int tile in targetTiles)
            {
                if (zombieX == tile.x && zombieY == tile.y)
                {
                    Debug.Log($"좀비가 1스킬(휘두르기)에 맞아 데미지를 입음!");
                    break; 
                }
            }
        }
    }

    private IEnumerator InstantSwingRotationRoutine()
    {
        if (swingEffectObject == null) yield break;

        float baseAngle = 0f;
        if (_lookDirection == Vector3.right) baseAngle = 0f;
        else if (_lookDirection == Vector3.up) baseAngle = 90f;
        else if (_lookDirection == Vector3.left) baseAngle = 180f;
        else if (_lookDirection == Vector3.down) baseAngle = 270f;

        swingEffectObject.SetActive(true);

        SetEffectTransform(baseAngle - 50f);
        yield return new WaitForSeconds(swingDuration / 3f);

        SetEffectTransform(baseAngle);
        yield return new WaitForSeconds(swingDuration / 3f);

        SetEffectTransform(baseAngle + 50f);
        yield return new WaitForSeconds(swingDuration / 3f);

        swingEffectObject.SetActive(false);
    }

    private void SetEffectTransform(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * 1.0f;
        swingEffectObject.transform.localPosition = offset;
        swingEffectObject.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void TryCounterAttack()
    {
        Debug.Log("인간 2스킬 카운터 스킬 발동 (K키)!");
        _counterCooldownTimer = counterCooldown; // 타이머 작동 시작!
        
        // 스킬을 누른 '첫 프레임'에 테두리를 0으로 깎고 연출 시작하도록 강제 설정
        if (counterBorderImage != null) counterBorderImage.fillAmount = 0f;

        if (_attackEffectCoroutine != null) StopCoroutine(_attackEffectCoroutine);
        _attackEffectCoroutine = StartCoroutine(ShowCounterEffectRoutine());

        Vector3 attackOneStepPos = transform.position + _lookDirection;
        Vector3 attackTwoStepPos = transform.position + (_lookDirection * 2);

        int t1X = Mathf.RoundToInt(attackOneStepPos.x);
        int t1Y = Mathf.RoundToInt(attackOneStepPos.y);
        int t2X = Mathf.RoundToInt(attackTwoStepPos.x);
        int t2Y = Mathf.RoundToInt(attackTwoStepPos.y);

        ZombieAIWithCounter[] zombies = Object.FindObjectsByType<ZombieAIWithCounter>(FindObjectsSortMode.None);
        foreach (ZombieAIWithCounter zombie in zombies)
        {
            int zombieX = Mathf.RoundToInt(zombie.transform.position.x);
            int zombieY = Mathf.RoundToInt(zombie.transform.position.y);

            if ((zombieX == t1X && zombieY == t1Y) || (zombieX == t2X && zombieY == t2Y))
            {
                zombie.OnGetHitByPlayer();
                break; 
            }
        }
    }

    private IEnumerator ShowCounterEffectRoutine()
    {
        if (counterEffectObject == null) yield break;

        counterEffectObject.transform.localPosition = _lookDirection * 1.5f;

        if (_lookDirection == Vector3.right) counterEffectObject.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        else if (_lookDirection == Vector3.left) counterEffectObject.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        else if (_lookDirection == Vector3.up) counterEffectObject.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        else if (_lookDirection == Vector3.down) counterEffectObject.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);

        counterEffectObject.SetActive(true);
        yield return new WaitForSeconds(counterDuration);
        counterEffectObject.SetActive(false);
    }

    private List<Vector2Int> GetFrontThreeTiles(int myX, int myY)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();
        if (_lookDirection == Vector3.up)
        {
            tiles.Add(new Vector2Int(myX - 1, myY + 1));
            tiles.Add(new Vector2Int(myX,     myY + 1));
            tiles.Add(new Vector2Int(myX + 1, myY + 1));

            tiles.Add(new Vector2Int(myX - 1, myY + 2));
            tiles.Add(new Vector2Int(myX,     myY + 2));
            tiles.Add(new Vector2Int(myX + 1, myY + 2));
        }
        else if (_lookDirection == Vector3.down)
        {
            tiles.Add(new Vector2Int(myX - 1, myY - 1));
            tiles.Add(new Vector2Int(myX,     myY - 1));
            tiles.Add(new Vector2Int(myX + 1, myY - 1));

            tiles.Add(new Vector2Int(myX - 1, myY - 2));
            tiles.Add(new Vector2Int(myX,     myY - 2));
            tiles.Add(new Vector2Int(myX + 1, myY - 2));
        }
        else if (_lookDirection == Vector3.left)
        {
            tiles.Add(new Vector2Int(myX - 1, myY + 1));
            tiles.Add(new Vector2Int(myX - 1, myY));
            tiles.Add(new Vector2Int(myX - 1, myY - 1));

            tiles.Add(new Vector2Int(myX - 2, myY + 1));
            tiles.Add(new Vector2Int(myX - 2, myY));
            tiles.Add(new Vector2Int(myX - 2, myY - 1));
        }
        else if (_lookDirection == Vector3.right)
        {
            tiles.Add(new Vector2Int(myX + 1, myY + 1));
            tiles.Add(new Vector2Int(myX + 1, myY));
            tiles.Add(new Vector2Int(myX + 1, myY - 1));

            tiles.Add(new Vector2Int(myX + 2, myY + 1));
            tiles.Add(new Vector2Int(myX + 2, myY));
            tiles.Add(new Vector2Int(myX + 2, myY - 1));
        }
        return tiles;
    }

    private void UpdateGridSortingOrder()
    {
        if (_spriteRenderer == null) return;

        int currentGridY = Mathf.RoundToInt(transform.position.y);
        _spriteRenderer.sortingOrder = 100 - currentGridY;
        
        if (swingEffectObject != null)
        {
            SpriteRenderer sr = swingEffectObject.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = _spriteRenderer.sortingOrder + 1;
        }
        if (counterEffectObject != null)
        {
            SpriteRenderer sr = counterEffectObject.GetComponent<SpriteRenderer>();
            // ★ 오타 수정: _renderer -> _spriteRenderer
            if (sr != null) sr.sortingOrder = _spriteRenderer.sortingOrder + 1; 
        }
    }
}