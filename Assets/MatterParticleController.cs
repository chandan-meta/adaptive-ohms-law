using UnityEngine;

public class MatterParticleController : MonoBehaviour
{
    public enum MatterState
    {
        Solid,
        Liquid,
        Gas
    }

    [Header("Matter State")]
    public MatterState state = MatterState.Solid;

    [Header("Play Settings")]
    public bool playOnStart = true;

    // ================= SOLID =================

    [Header("Solid Settings")]
    [Tooltip("How far each particle vibrates from its original position.")]
    public float solidVibrationAmount = 0.01f;

    [Tooltip("How fast solid particles vibrate.")]
    public float solidVibrationSpeed = 5f;


    // ================= LIQUID =================

    [Header("Liquid Settings")]
    public float liquidMoveSpeed = 0.12f;

    [Tooltip("Size of the area in which liquid particles can move.")]
    public Vector3 liquidAreaSize = new Vector3(0.8f, 0.4f, 0.8f);

    [Tooltip("How often particles change direction.")]
    public float liquidDirectionChangeTime = 1f;


    // ================= GAS =================

    [Header("Gas Settings")]
    public float gasMoveSpeed = 0.5f;

    [Tooltip("Size of the invisible box containing the gas particles.")]
    public Vector3 gasAreaSize = new Vector3(1.5f, 1.5f, 1.5f);

    [Tooltip("How often gas particles randomly change direction.")]
    public float gasDirectionChangeTime = 2f;


    // ================= INTERNAL =================

    private Transform[] particles;

    private Vector3[] startLocalPositions;
    private Vector3[] directions;

    private float[] timers;
    private float[] randomOffsets;

    private bool isPlaying;


    private void Start()
    {
        GetParticles();

        isPlaying = playOnStart;
    }


    private void Update()
    {
        if (!isPlaying)
            return;

        if (particles == null || particles.Length == 0)
            return;

        switch (state)
        {
            case MatterState.Solid:
                UpdateSolid();
                break;

            case MatterState.Liquid:
                UpdateLiquid();
                break;

            case MatterState.Gas:
                UpdateGas();
                break;
        }
    }


    // =========================================================
    // GET ALL CHILD PARTICLES
    // =========================================================

    private void GetParticles()
    {
        int childCount = transform.childCount;

        particles = new Transform[childCount];

        startLocalPositions = new Vector3[childCount];

        directions = new Vector3[childCount];

        timers = new float[childCount];

        randomOffsets = new float[childCount];


        for (int i = 0; i < childCount; i++)
        {
            particles[i] = transform.GetChild(i);

            startLocalPositions[i] =
                particles[i].localPosition;

            directions[i] =
                Random.insideUnitSphere.normalized;

            timers[i] =
                Random.Range(0.5f, 1.5f);

            randomOffsets[i] =
                Random.Range(0f, 100f);
        }
    }


    // =========================================================
    // SOLID
    // =========================================================

    private void UpdateSolid()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            float offset = randomOffsets[i];

            float x =
                Mathf.Sin(
                    (Time.time + offset)
                    * solidVibrationSpeed
                ) * solidVibrationAmount;

            float y =
                Mathf.Cos(
                    (Time.time + offset * 0.7f)
                    * solidVibrationSpeed
                ) * solidVibrationAmount;

            float z =
                Mathf.Sin(
                    (Time.time + offset * 1.3f)
                    * solidVibrationSpeed
                ) * solidVibrationAmount;


            particles[i].localPosition =
                startLocalPositions[i]
                + new Vector3(x, y, z);
        }
    }


    // =========================================================
    // LIQUID
    // =========================================================

    private void UpdateLiquid()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            timers[i] -= Time.deltaTime;


            if (timers[i] <= 0f)
            {
                // Mostly sideways movement for liquid
                directions[i] = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-0.25f, 0.25f),
                    Random.Range(-1f, 1f)
                ).normalized;


                timers[i] =
                    liquidDirectionChangeTime
                    + Random.Range(-0.3f, 0.3f);
            }


            particles[i].localPosition +=
                directions[i]
                * liquidMoveSpeed
                * Time.deltaTime;


            CheckLiquidBounds(i);
        }
    }


    // =========================================================
    // LIQUID BOUNDS
    // =========================================================

    private void CheckLiquidBounds(int index)
    {
        Vector3 position =
            particles[index].localPosition;

        Vector3 center = Vector3.zero;


        float halfX = liquidAreaSize.x * 0.5f;
        float halfY = liquidAreaSize.y * 0.5f;
        float halfZ = liquidAreaSize.z * 0.5f;


        if (position.x > center.x + halfX)
        {
            position.x = center.x + halfX;

            directions[index].x =
                -Mathf.Abs(directions[index].x);
        }
        else if (position.x < center.x - halfX)
        {
            position.x = center.x - halfX;

            directions[index].x =
                Mathf.Abs(directions[index].x);
        }


        if (position.y > center.y + halfY)
        {
            position.y = center.y + halfY;

            directions[index].y =
                -Mathf.Abs(directions[index].y);
        }
        else if (position.y < center.y - halfY)
        {
            position.y = center.y - halfY;

            directions[index].y =
                Mathf.Abs(directions[index].y);
        }


        if (position.z > center.z + halfZ)
        {
            position.z = center.z + halfZ;

            directions[index].z =
                -Mathf.Abs(directions[index].z);
        }
        else if (position.z < center.z - halfZ)
        {
            position.z = center.z - halfZ;

            directions[index].z =
                Mathf.Abs(directions[index].z);
        }


        particles[index].localPosition = position;
    }


    // =========================================================
    // GAS
    // =========================================================

    private void UpdateGas()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            timers[i] -= Time.deltaTime;


            if (timers[i] <= 0f)
            {
                directions[i] =
                    Random.insideUnitSphere.normalized;


                timers[i] =
                    gasDirectionChangeTime
                    + Random.Range(-0.5f, 0.5f);
            }


            particles[i].localPosition +=
                directions[i]
                * gasMoveSpeed
                * Time.deltaTime;


            CheckGasBounds(i);
        }
    }


    // =========================================================
    // GAS BOUNDS
    // =========================================================

    private void CheckGasBounds(int index)
    {
        Vector3 position =
            particles[index].localPosition;


        float halfX = gasAreaSize.x * 0.5f;
        float halfY = gasAreaSize.y * 0.5f;
        float halfZ = gasAreaSize.z * 0.5f;


        if (position.x > halfX)
        {
            position.x = halfX;

            directions[index].x =
                -Mathf.Abs(directions[index].x);
        }
        else if (position.x < -halfX)
        {
            position.x = -halfX;

            directions[index].x =
                Mathf.Abs(directions[index].x);
        }


        if (position.y > halfY)
        {
            position.y = halfY;

            directions[index].y =
                -Mathf.Abs(directions[index].y);
        }
        else if (position.y < -halfY)
        {
            position.y = -halfY;

            directions[index].y =
                Mathf.Abs(directions[index].y);
        }


        if (position.z > halfZ)
        {
            position.z = halfZ;

            directions[index].z =
                -Mathf.Abs(directions[index].z);
        }
        else if (position.z < -halfZ)
        {
            position.z = -halfZ;

            directions[index].z =
                Mathf.Abs(directions[index].z);
        }


        particles[index].localPosition = position;
    }


    // =========================================================
    // PUBLIC FUNCTIONS
    // =========================================================

    public void StartMovement()
    {
        isPlaying = true;
    }


    public void StopMovement()
    {
        isPlaying = false;
    }


    public void SetSolid()
    {
        state = MatterState.Solid;

        SaveCurrentPositions();
    }


    public void SetLiquid()
    {
        state = MatterState.Liquid;

        RandomizeDirections();
    }


    public void SetGas()
    {
        state = MatterState.Gas;

        RandomizeDirections();
    }


    // =========================================================
    // HELPERS
    // =========================================================

    private void SaveCurrentPositions()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            startLocalPositions[i] =
                particles[i].localPosition;
        }
    }


    private void RandomizeDirections()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            directions[i] =
                Random.insideUnitSphere.normalized;

            timers[i] =
                Random.Range(0.5f, 1.5f);
        }
    }


    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        if (state == MatterState.Liquid)
        {
            Gizmos.DrawWireCube(
                Vector3.zero,
                liquidAreaSize
            );
        }

        if (state == MatterState.Gas)
        {
            Gizmos.DrawWireCube(
                Vector3.zero,
                gasAreaSize
            );
        }
    }
}