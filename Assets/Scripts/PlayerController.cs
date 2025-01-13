using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _movementSpeed = 10.0f;
    [SerializeField] private float _sideSpeed = 5f;
    [SerializeField] private float _swipeThreshold = 0.1f;
    [SerializeField] private float _jumpForce = 3f;
    [SerializeField] private GameObject _deathEffect;
    private float _sideMove = 2.5f;
    private Vector3 velocity = Vector3.zero;

    private float _maxLeft = -2.5f;
    private float _maxRight = 2.5f;

    private PlayerInput _playerInput;
    private Rigidbody _rb;

    private InputAction _moveAction;
    private InputAction _jumpAction;

    public bool IsRunning = true;
    private bool _canMove;
    private bool _isGrounded;
    private bool _isAudioLoaded = false;

    public void SetAudioLoaded()
    {
        Debug.Log("Audio loaded - Movement enabled");
        _isAudioLoaded = true;
    }

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput == null)
        {
            Debug.LogError("PlayerInput component not found on GameObject");
        }
        else
        {
            _moveAction = _playerInput.actions["Move"];
            Debug.Log("Move action found: " + _moveAction);
            _jumpAction = _playerInput.actions["Jump"];
            Debug.Log("Jump action found: " + _jumpAction);
        }
    }

    private void OnEnable()
    {
        _moveAction.performed += Move;
        _jumpAction.performed += Jump;
    }

    private void OnDisable()
    {
        _moveAction.performed -= Move;
        _jumpAction.performed -= Jump;
    }

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _canMove = true;
    }

    private void OnCollisionExit(Collision other) 
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            _isGrounded = false;
        }
    }

    private void OnCollisionEnter(Collision other) 
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            _isGrounded = true;
        }
        
        if (other.gameObject.CompareTag("Obstacle"))
        {
            _canMove = false;
            IsRunning = false;
            Instantiate(_deathEffect, transform.position, Quaternion.identity);
            GetComponentInChildren<Animator>().SetTrigger("Stop");
            GameManager.Instance.EndGame(false);
        }
    }

    private void Update()
    {
        if (_isAudioLoaded && IsRunning)
        {
            transform.Translate(Vector3.forward * _movementSpeed * Time.deltaTime);
        }
    }

    void Move(InputAction.CallbackContext context)
    {
        Vector2 delta = context.ReadValue<Vector2>();

        if (delta.x < -_swipeThreshold || delta.x > _swipeThreshold)
        {
            if (_canMove)
            {
                float targetX = transform.position.x + (delta.x > 0 ? _sideMove : -_sideMove); 
                targetX = Mathf.Clamp(targetX, _maxLeft, _maxRight); 
                StartCoroutine(MovementRoutine(targetX));
            }
        }
    }

    void Jump(InputAction.CallbackContext context)
    {
        Debug.Log("Jump");

        if (_isGrounded)
        {
            _rb.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
        }
    }

    IEnumerator MovementRoutine(float targetDestination)
    {
        _canMove = false; 
        while (Mathf.Abs(transform.position.x - targetDestination) > 0.1f)
        {
            transform.position = Vector3.SmoothDamp(transform.position, new Vector3(targetDestination, transform.position.y, transform.position.z), ref velocity, _sideSpeed * Time.deltaTime);
            yield return null;
        }
        _canMove = true;
    }
}
