using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// Agent contrôlé par ML-Agents représentant un chasseur dans l'environnement.
/// Ce chasseur peut se déplacer et tourner selon les actions reçues,
/// et interagir avec les proies pour les capturer.
/// </summary>
public class HunterAgentFinal : Agent
{
    #region Variables

    // === Mouvement ===

    /// <summary>
    /// Vitesse de déplacement en avant du chasseur.
    /// </summary>
    public float moveSpeed = 3f;

    /// <summary>
    /// Vitesse de rotation autour de l’axe Y.
    /// </summary>
    public float rotationSpeed = 200f;

    /// <summary>
    /// Position précédente de l’agent.
    /// </summary>
    private Vector3 lastPosition;

    /// <summary>
    /// Rotation accumulée, utilisée pour détecter les comportements de rotation.
    /// </summary>
    private float accumulatedRotation = 0f;

    /// <summary>
    /// Facteur de décroissance de la rotation accumulée.
    /// </summary>
    private float rotationDecay = 0.95f;

    /// <summary>
    /// Indique si le chasseur est activé (autorisé à se déplacer).
    /// </summary>
    private bool active = false;

    // === Perception ===

    public CustomRayPerception rayPerception;
    public int observationSize = 56;

    // === Communication ===

    public bool enableCommunication = true;
    public string agentId;
    private List<string> tagToCommunicate;

    // === État de l’agent ===

    /// <summary>
    /// Indique si l’agent est "mort".
    /// </summary>
    private bool isDead = false;

    // === Exploration / Historique de position ===

    private Queue<Vector3> positionHistory;
    private int historySize = 100;
    private float minExplorationRadius = 3f;

    // === Références ===

    /// <summary>
    /// Référence vers l’environnement principal, pour signaler les événements.
    /// </summary>
    private HunterPreyEnvFinal env;

    #endregion

    /// <summary>
    /// Attribue l'environnement au chasseur.
    /// </summary>
    /// <param name="e">L’environnement principal HunterPreyEnv.</param>
    public void SetEnv(HunterPreyEnvFinal e) => env = e;

    #region Agent

    /// <summary>
    /// Réinitialise l’agent au début d’un nouvel épisode.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        active = false; // Les chasseurs sont inactifs au début
        lastPosition = transform.position;
        positionHistory.Clear();
        accumulatedRotation = 0;
    }

    /// <summary>
    /// Active le chasseur pour lui permettre de bouger.
    /// </summary>
    public void Activate() => active = true;

    /// <summary>
    /// Désactive le chasseur, l’empêchant de bouger.
    /// </summary>
    public void Deactivate() => active = false;

    public override void Initialize()
    {
        agentId = System.Guid.NewGuid().ToString().Substring(0, 8); // ex: "a3f2c0d1"
        tagToCommunicate = new List<string>
        {
            "Prey",
            //"EnergyPrey",
            //"BoostSpeed",
            //"Camouflage"
        };
        positionHistory = new Queue<Vector3>();

    }

    public new void AddReward(float reward)
    {
        if (!isDead)
            base.AddReward(reward);
    }

    public void SetAgentDead()
    {
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
    /// Collecte les observations de l’environnement à partir de CustomRayPerception.
    /// </summary>
    /// <param name="sensor">Le capteur utilisé pour collecter les observations.</param>
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
                env.commBuffer.SendMessageInfo(agentId, message, true);
            }

            foreach (float val in obs[0])
            {
                sensor.AddObservation(val);
                observationCount++;
            }
        }

        if (enableCommunication)
        {
            sensor.AddObservation(transform.localPosition);
            observationCount += 3;

            var messages = env.commBuffer.GetAllMessages(true);
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
    /// Applique les actions reçues par le modèle ML.
    /// L’agent peut tourner et avancer.
    /// </summary>
    /// <param name="actions">Actions continues : [rotation, avance].</param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (!active || isDead) return;

        float forward = Mathf.Clamp(actions.ContinuousActions[0], 0f, 1f);
        float rotation = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        // Appliquer la rotation
        transform.Rotate(Vector3.up, rotation * rotationSpeed * Time.deltaTime);

        // Appliquer la translation vers l’avant
        transform.position += transform.forward * forward * moveSpeed * Time.deltaTime;

        //AddReward(-0.001f * env.GetTimeFloat());

        if (forward < 0.2f)
        {
            AddReward(-0.001f);
        }
        else
        {
            AddReward(0.001f);
        }

        float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        if (distanceMoved < 0.05f)
        {
            AddReward(-0.005f); // Pénalité si l'agent n'explore vraiment pas
        }

        lastPosition = transform.position;

        //float minHunterDistance = float.MaxValue;
        //foreach (var hunter in env.GetAllPreys())
        //{
        //    float dist = Vector3.Distance(transform.position, hunter.transform.position);
        //    if (dist < minHunterDistance)
        //        minHunterDistance = dist;
        //}

        //// Récompense plus grande quand distance est plus petite
        //AddReward(0.001f * (1f - Mathf.Clamp01(minHunterDistance / 20f)));

        //if (minHunterDistance < 2f)
        //{
        //    AddReward(0.05f); // Bonus s'il est très proche
        //}

        //float minHunterDistance = float.MaxValue;
        //foreach (var hunter in env.GetAllHunters())
        //{
        //    if(hunter.gameObject == this.gameObject) continue;
        //    float dist = Vector3.Distance(transform.position, hunter.transform.position);
        //    if (dist < minHunterDistance)
        //        minHunterDistance = dist;
        //}

        //if (minHunterDistance < 5f)
        //{
        //    AddReward(-0.05f);
        //}

        //// Mémorisation de la position
        //positionHistory.Enqueue(transform.position);
        //if (positionHistory.Count > historySize)
        //    positionHistory.Dequeue();

        //// Calcul de la distance moyenne par rapport au centre de la zone visitée
        //Vector3 avgPos = Vector3.zero;
        //foreach (var pos in positionHistory)
        //    avgPos += pos;
        //avgPos /= positionHistory.Count;

        //float radius = 0f;
        //foreach (var pos in positionHistory)
        //    radius += Vector3.Distance(avgPos, pos);
        //radius /= positionHistory.Count;

        //// Si le rayon moyen est faible, pénaliser
        //if (radius < minExplorationRadius)
        //{
        //    AddReward(reward: -0.002f);
        //}

        //accumulatedRotation = accumulatedRotation * rotationDecay + Mathf.Abs(rotation);
        //if (accumulatedRotation > 10f)
        //{
        //    AddReward(-0.001f); // Pénalise le comportement trop tournoyant
        //}

    }

    #endregion

    #region Physics

    /// <summary>
    /// Détecte la collision avec une proie. Capture la proie si contact.
    /// </summary>
    /// <param name="collision">Collision détectée avec un autre objet.</param>
    private void OnCollisionEnter(Collision collision)
    {
        if(isDead) return;

        if (collision.gameObject.CompareTag("Prey"))
        {
            // Récompense positive pour la capture
            AddReward(2f);

            // Notifie l’environnement qu’une proie a été attrapée
            env.OnPreyCaught(collision.gameObject);
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-2f);
            //env.OnHunterEnterDanger(this);
        }
        else if (collision.gameObject.CompareTag("Obstacle"))
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
            env.OnHunterEnterDanger(this);
        }
        if (other.CompareTag("SpeedBoost") || other.CompareTag("Camouflage") || other.CompareTag("EnergyPrey"))
        {
            AddReward(0.1f); // petite récompense stratégique
            env.OnBonusDestroyed(other.gameObject);
        }
    }

    #endregion
}
