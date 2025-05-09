using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.LightTransport;
using static Unity.Burst.Intrinsics.X86.Avx;

/// <summary>
/// Agent contrôlé par ML-Agents représentant un chasseur dans l'environnement.
/// Ce chasseur peut se déplacer et tourner selon les actions reçues,
/// et interagir avec les proies pour les capturer.
/// </summary>
public class HunterAgentFinal : Agent
{
    /// <summary>
    /// Vitesse de déplacement en avant du chasseur.
    /// </summary>
    public float moveSpeed = 3f;

    /// <summary>
    /// Vitesse de rotation autour de l’axe Y.
    /// </summary>
    public float rotationSpeed = 200f;

    /// <summary>
    /// Indique si le chasseur est activé (autorisé à se déplacer).
    /// </summary>
    private bool active = false;

    public CustomRayPerception rayPerception;
    public int observationSize = 56;
    public bool enableCommunication = true;

    /// <summary>
    /// Référence vers l’environnement principal, pour signaler les événements.
    /// </summary>
    private HunterPreyEnvFinal env;

    /// <summary>
    /// Attribue l'environnement au chasseur.
    /// </summary>
    /// <param name="e">L’environnement principal HunterPreyEnv.</param>
    public void SetEnv(HunterPreyEnvFinal e) => env = e;

    /// <summary>
    /// Réinitialise l’agent au début d’un nouvel épisode.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        active = false; // Les chasseurs sont inactifs au début
    }

    /// <summary>
    /// Active le chasseur pour lui permettre de bouger.
    /// </summary>
    public void Activate() => active = true;

    /// <summary>
    /// Désactive le chasseur, l’empêchant de bouger.
    /// </summary>
    public void Deactivate() => active = false;

    private bool isDead = false; // Indique si l'agent est "mort"

    public string agentId;

    private List<string> tagToCommunicate;
    public override void Initialize()
    {
        agentId = System.Guid.NewGuid().ToString().Substring(0, 8); // ex: "a3f2c0d1"
        tagToCommunicate = new List<string>
        {
            "Prey",
            "EnergyPrey",
            "BoostSpeed",
            "Camouflage"
        };
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
                    localPosition = transform.localPosition
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
                    observationCount += 3;

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
    }

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
            AddReward(1f);

            // Notifie l’environnement qu’une proie a été attrapée
            env.OnPreyCaught(collision.gameObject);
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            SetReward(-1f);
            env.OnHunterEnterDanger(this);
        }
        else if (collision.gameObject.CompareTag("Obstacle"))
        {
            AddReward(-0.2f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (other.CompareTag("Danger")) 
        {
            AddReward(-1f);
            env.OnHunterEnterDanger(this);
        }
        if (other.CompareTag("SpeedBoost") || other.CompareTag("Camouflage"))
        {
            AddReward(0.2f); // petite récompense stratégique
            env.OnBonusDestroyed(other.gameObject);
        }
    }
}
