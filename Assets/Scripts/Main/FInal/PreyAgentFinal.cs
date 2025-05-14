using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// Agent représentant une proie dans l’environnement.
/// Se déplace pour échapper aux chasseurs et maximise sa survie.
/// </summary>
public class PreyAgentFinal : Agent
{

    #region Variables

    // === Mouvement ===

    /// <summary>
    /// Vitesse de déplacement vers l’avant.
    /// </summary>
    public float moveSpeed = 3f;

    /// <summary>
    /// Vitesse de rotation autour de l’axe Y.
    /// </summary>
    public float rotationSpeed = 200f;

    private float baseMoveSpeed;
    private Vector3 lastPosition;

    // === Énergie ===

    private float energy;
    public float minEnergy = 5f;
    public float maxEnergy = 15f;
    public float energyDecayRate = 1f;         // Énergie perdue par seconde
    public float energyGainAmount = 5f;        // Énergie gagnée en touchant une proie spéciale

    // === Perception ===

    public CustomRayPerception rayPerception;
    public CustomRayPerception rayPerceptionBack;
    public int observationSize = 56;

    // === Communication ===

    public bool enableCommunication = true;
    private List<string> tagToCommunicate;
    public string agentId;

    // === Capacités spéciales ===

    public float boostedSpeed = 6f;
    public float boostDuration = 5f;
    public float camouflageDuration = 5f;

    private bool isSpeedBoosted = false;
    private float timeStartBoostSpeed;

    private bool isInvisible = false;
    private float timeStartInvisible;

    private Coroutine speedBoostCoroutine;
    private Coroutine camouflageCoroutine;

    // === Apparence & Rendu ===

    public Renderer agentRenderer;
    private Material defaultMaterial;
    public Material camouflageMaterial;

    // === Références ===

    /// <summary>
    /// Référence vers l’environnement global.
    /// </summary>
    private HunterPreyEnvFinal env;

    #endregion

    /// <summary>
    /// Associe l’environnement à cet agent.
    /// </summary>
    /// <param name="e">L’environnement principal HunterPreyEnv.</param>
    public void SetEnv(HunterPreyEnvFinal e) => env = e;

    #region Agent

    /// <summary>
    /// Initialise l'agent, id unique, materiel de base etc
    /// </summary>
    public override void Initialize()
    {
        agentId = System.Guid.NewGuid().ToString().Substring(0, 8); // ex: "a3f2c0d1"
        tagToCommunicate = new List<string>
        {
            "Hunter",
            //"EnergyPrey",
            //"BoostSpeed",
            //"Camouflage"
        };
        defaultMaterial = agentRenderer.material;
        baseMoveSpeed = moveSpeed;
    }

    /// <summary>
    /// Réinitialisation de l’agent au début d’un épisode.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        lastPosition = transform.position;
        gameObject.tag = "Prey";
        isInvisible = false;
        isSpeedBoosted = false;
        moveSpeed = baseMoveSpeed;
        agentRenderer.material = defaultMaterial;
        StopAllCoroutines();
        energy = Random.Range(minEnergy, maxEnergy);
        // Peut être complété si besoin : repositionner, reset timer, etc.
    }

    private bool isDead = false; // Indique si l'agent est "mort"

    public new void AddReward(float reward)
    {
        if (!isDead)
            base.AddReward(reward);
    }

    public void SetAgentDead()
    {
        StopAllCoroutines();
        isDead = true;
        gameObject.GetComponent<Collider>().enabled = false; // Ignore les collisions
        SetChildrenActive(false);
    }

    public void SetAgentAlive()
    {
        isDead = false;
        gameObject.GetComponent<Collider>().enabled = true;
        SetChildrenActive(true);
    }

    // Fonction utilitaire pour activer/désactiver tous les enfants
    private void SetChildrenActive(bool state)
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(state);
        }
    }

    /// <summary>
    /// Collecte les observations de l’environnement (à compléter).
    /// Permet à l’agent de percevoir les éléments autour.
    /// </summary>
    /// <param name="sensor">Le capteur d’observations.</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        int observationCount = 0;

        if (isDead)
        {
            sensor.AddObservation(new float[observationSize]);
            observationCount += observationSize;
            // Debug.Log("Observation count: " + observationCount);
            return;
        }

        if (isInvisible)
        {
            sensor.AddObservation((Time.time - timeStartInvisible) / camouflageDuration);
        }
        else
        {
            sensor.AddObservation(0);
        }
        observationCount++;

        if (isSpeedBoosted)
        {
            sensor.AddObservation((Time.time - timeStartBoostSpeed) / boostDuration);
        }
        else
        {
            sensor.AddObservation(0);
        }
        observationCount++;


        sensor.AddObservation(energy / maxEnergy); // Normalisé entre 0 et 1
        observationCount++;

        sensor.AddObservation(env.GetTimeFloat());
        observationCount++;

        if (rayPerception != null)
        {
            float[][] obs = rayPerception.GetObservations(tagToCommunicate);
            
            if (enableCommunication)
            {
                CommunicationBuffer.Message message = new CommunicationBuffer.Message
                {
                    rayResults = obs[1],
                    localPosition = transform.localPosition,
                    rotation = transform.eulerAngles.y / 360f
                };
                env.commBuffer.SendMessageInfo(agentId, message, false);
            }

            foreach (float val in obs[0])
            {
                sensor.AddObservation(val);
                observationCount++;
            }

            //obs = rayPerceptionBack.GetObservations(tagToCommunicate);
            //foreach (float val in obs[0])
            //{
            //    sensor.AddObservation(val);
            //    observationCount++;
            //}
        }

        if (enableCommunication)
        {
            sensor.AddObservation(transform.localPosition);
            observationCount += 3;

            var messages = env.commBuffer.GetAllMessages(false);
            foreach (var entry in messages)
            {
                if (entry.Key != agentId)
                {
                    sensor.AddObservation(entry.Value.localPosition);
                    sensor.AddObservation(entry.Value.rotation);
                    observationCount += 4;

                    foreach (var r in entry.Value.rayResults)
                    {
                        sensor.AddObservation(r);
                        observationCount++;
                    }
                }
            }
        }

        //Debug.Log("Total observations: " + observationCount);

        if (observationSize > observationCount)
        {
            int paddingSize = observationSize - observationCount;
            float[] padding = new float[paddingSize];
            sensor.AddObservation(padding);
            //Debug.LogWarning("Padding applied: " + paddingSize + " zeros.");
        }
        else if (observationSize < observationCount)
        {
            Debug.LogError("Too many observations! Expected: " + observationSize + ", Got: " + observationCount);
        }
    }


    /// <summary>
    /// Reçoit et applique les actions continues.
    /// La proie peut tourner et avancer.
    /// Une petite récompense est donnée à chaque frame pour survivre plus longtemps.
    /// </summary>
    /// <param name="actions">Actions continues : [rotation, mouvement].</param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (isDead) return;

        float forward = Mathf.Clamp(actions.ContinuousActions[0], 0f, 1f);
        float rotation = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        // Appliquer la rotation
        transform.Rotate(Vector3.up, rotation * rotationSpeed * Time.deltaTime);

        // Appliquer l'avance
        transform.position += transform.forward * forward * moveSpeed * Time.deltaTime;

        // Récompense pour avoir survécu un pas de temps
        //AddReward(0.001f * env.GetTimeFloat());

        // Diminution de l'énergie
        energy -= energyDecayRate * Time.deltaTime;

        // Punition si l'énergie tombe à zéro
        if (energy <= 0f)
        {
            //SetReward(-1f); // ou une récompense personnalisée
            //env.OnPreyEnergyDepleted(this); // à implémenter dans l’environnement

            AddReward(-0.01f);
        }

        //float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        //if (distanceMoved < 0.05f)
        //{
        //    AddReward(-0.005f); // Pénalité si l'agent n'explore vraiment pas
        //}

        float minHunterDistance = float.MaxValue;
        foreach (var hunter in env.GetAllHunters())
        {
            float dist = Vector3.Distance(transform.position, hunter.transform.position);
            if (dist < minHunterDistance)
                minHunterDistance = dist;
        }
        AddReward(0.001f * Mathf.Clamp01(minHunterDistance / 20f));
        if (minHunterDistance < 2f)
        {
            AddReward(-0.5f); // Punir si trop proche
        }

    }

#endregion

    #region Physics

    /// <summary>
    /// Détecte les collisions.
    /// </summary>
    /// <param name="collision">Collision détectée avec un autre objet.</param>
    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-2f);
            //env.OnPreyEnterDanger(this);
        }

        if (collision.gameObject.CompareTag("Obstacle"))
        {
            AddReward(-2f);
        }
    }

    /// <summary>
    /// Detecte les collisions trigger, avec les bonus, les pieges, et la limite de la map.
    /// </summary>
    /// <param name="other">Trigger détectée avec un autre objet.</param>
    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (other.CompareTag("Danger") || other.CompareTag("LimitMap"))
        {
            SetReward(-10f);
            env.OnPreyEnterDanger(this);
        }
        else if (other.CompareTag("EnergyPrey"))
        {
            energy = Mathf.Min(energy + energyGainAmount, maxEnergy);
            AddReward(1f);
            env.OnPreyEnergy(other.gameObject);
        }
        else if (other.CompareTag("SpeedBoost"))
        {
            AddReward(1f);
            env.OnSpeedBoostCollected(other.gameObject);
            if (speedBoostCoroutine != null) StopCoroutine(speedBoostCoroutine);
            speedBoostCoroutine = StartCoroutine(SpeedBoostRoutine());
        }
        else if (other.CompareTag("Camouflage"))
        {
            AddReward(1f);
            env.OnCamouflageCollected(other.gameObject);
            if (camouflageCoroutine != null) StopCoroutine(camouflageCoroutine);
            camouflageCoroutine = StartCoroutine(CamouflageRoutine());
        }
    }

    #endregion

    #region Coroutines

    private IEnumerator SpeedBoostRoutine()
    {
        timeStartBoostSpeed = Time.time;
        isSpeedBoosted = true;
        moveSpeed = boostedSpeed;
        yield return new WaitForSeconds(boostDuration);
        moveSpeed = baseMoveSpeed;
        isSpeedBoosted = false;
    }

    private IEnumerator CamouflageRoutine()
    {
        timeStartInvisible = Time.time;
        isInvisible = true;
        agentRenderer.material = camouflageMaterial;
        gameObject.tag = "InvisiblePrey"; // Pour ne plus être détectée par les chasseurs
        yield return new WaitForSeconds(camouflageDuration);
        gameObject.tag = "Prey";
        agentRenderer.material = defaultMaterial;
        isInvisible = false;
    }

    #endregion

    //public void SetWin()
    //{
    //    if (!isDead)
    //        AddReward(10f);
    //}
}
