using UnityEngine;

// Throwable that creates a carpet when it lands (like Jhin's mine throw)
public class LuxCarpetThrowable : MonoBehaviour
{
    private LuxController _owner;
    private float _damage;
    private float _slowAmount;
    private float _radius;
    private float _duration;
    private float _throwForce;
    private bool _landed = false;
    private Rigidbody _rb;

    public void Init(LuxController owner, float damage, float slowAmount, float radius, float duration, float throwForce, Vector3 throwDirection)
    {
        _owner = owner;
        _damage = damage;
        _slowAmount = slowAmount;
        _radius = radius;
        _duration = duration;
        _throwForce = throwForce;

        _rb = GetComponent<Rigidbody>();
        if (_rb != null)
        {
            _rb.AddForce(throwDirection * _throwForce, ForceMode.Impulse);
        }

        // Auto destroy if never lands
        Invoke(nameof(DestroySelf), 10f);
    }

    void FixedUpdate()
    {
        if (!_landed)
        {
            CheckGroundCollision();
        }
    }

    void CheckGroundCollision()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 0.5f);

        foreach (Collider collider in colliders)
        {
            if (collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                HandleLanding();
                break;
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!_landed && collision.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            HandleLanding();
        }
    }

    void HandleLanding()
    {
        _landed = true;

        if (_rb != null)
        {
            _rb.velocity = Vector3.zero;
            _rb.isKinematic = true;
        }

        // Create the carpet
        CreateCarpet();

        // Destroy throwable
        Destroy(gameObject);
    }

    void CreateCarpet()
    {
        // Create carpet object
        GameObject carpetObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        carpetObj.name = "LuxCarpet";
        carpetObj.transform.position = transform.position;
        carpetObj.transform.localScale = new Vector3(_radius * 2f, 0.05f, _radius * 2f);

        // Remove default collider, add trigger
        var defaultCol = carpetObj.GetComponent<Collider>();
        if (defaultCol != null) Object.Destroy(defaultCol);

        var triggerCol = carpetObj.AddComponent<SphereCollider>();
        triggerCol.isTrigger = true;
        triggerCol.radius = 0.5f; // Normalized to scale

        // Style
        var mr = carpetObj.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(1f, 1f, 0.4f, 0.6f); // Bright yellow, semi-transparent
            mr.material = mat;
        }

        // Add carpet behavior
        var carpet = carpetObj.AddComponent<LuxCarpet>();
        carpet.Init(_owner, _damage, _slowAmount, _radius, _duration);

        Debug.Log("Lux carpet created!");
    }

    void DestroySelf()
    {
        Destroy(gameObject);
    }
}
