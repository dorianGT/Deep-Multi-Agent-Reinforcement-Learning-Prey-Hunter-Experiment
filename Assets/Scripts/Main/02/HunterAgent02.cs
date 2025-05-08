using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.LightTransport;
using static Unity.Burst.Intrinsics.X86.Avx;

/// <summary>
/// Agent contr�l� par ML-Agents repr�sentant un chasseur dans l'environnement.
/// Ce chasseur peut se d�placer et tourner selon les actions re�ues,
/// et interagir avec les proies pour les capturer.
/// </summary>
public class HunterAgent02 : Agent
{
    /// <summary>
    /// Vitesse de d�placement en avant du chasseur.
    /// </summary>
    public float moveSpeed = 3f;

    /// <summary>
    /// Vitesse de rotation autour de l�axe Y.
    /// </summary>
    public float rotationSpeed = 200f;

    /// <summary>
    /// Indique si le chasseur est activ� (autoris� � se d�placer).
    /// </summary>
    private bool active = false;

    public CustomRayPerception rayPerception;
    public int observationSize = 56;
    public bool enableCommunication = true;

    /// <summary>
    /// R�f�rence vers l�environnement principal, pour signaler les �v�nements.
    /// </summary>
    private HunterPreyEnv02 env;

    /// <summary>
    /// Attribue l'environnement au chasseur.
    /// </summary>
    /// <param name="e">L�environnement principal HunterPreyEnv.</param>
    public void SetEnv(HunterPreyEnv02 e) => env = e;

    /// <summary>
    /// R�initialise l�agent au d�but d�un nouvel �pisode.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        active = false; // Les chasseurs sont inactifs au d�but
    }

    /// <summary>
    /// Active le chasseur pour lui permettre de bouger.
    /// </summary>
    public void Activate() => active = true;

    /// <summary>
    /// D�sactive le chasseur, l�emp�chant de bouger.
    /// </summary>
    public void Deactivate() => active = false;

    private bool isDead = false; // Indique si l'agent est "mort"

    public string agentId;

    public override void Initialize()
    {
        agentId = System.Guid.NewGuid().ToString().Substring(0, 8); // ex: "a3f2c0d1"
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

    // Fonction utilitaire pour activer/d�sactiver tous les enfants
    private void SetChildrenActive(bool state)
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(state);
        }
    }

    /// <summary>
    /// Collecte les observations de l�environnement � partir de CustomRayPerception.
    /// </summary>
    /// <param name="sensor">Le capteur utilis� pour collecter les observations.</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        if (isDead)
        {
            sensor.AddObservation(new float[observationSize]);
            return;
        }


        if (rayPerception != null)
        {
            float[][] obs = rayPerception.GetObservations("Prey");
            CommunicationBuffer.Message message = new CommunicationBuffer.Message
            {
                rayResults = obs[1],
                localPosition = transform.localPosition
            };
            env.commBuffer.SendMessageInfo(agentId, message,true);
            foreach (float val in obs[0])
            {
                sensor.AddObservation(val);
            }
        }

        if (enableCommunication)
        {
            sensor.AddObservation(transform.localPosition);
            int tmp2 = 0;
            var messages = env.commBuffer.GetAllMessages(true);
            foreach (var entry in messages)
            {
                if (entry.Key != agentId) // Ignorer soi-m�me
                {
                    tmp2++;
                    sensor.AddObservation(entry.Value.localPosition);
                    foreach (var r in entry.Value.rayResults)
                    {
                        sensor.AddObservation(r);
                    }
                }
            }
            if (tmp2 != env.hunterCount-1)
            {
                sensor.AddObservation(new float[(env.hunterCount-1-tmp2)*17]);
            }
        }
    }

    /// <summary>
    /// Applique les actions re�ues par le mod�le ML.
    /// L�agent peut tourner et avancer.
    /// </summary>
    /// <param name="actions">Actions continues : [rotation, avance].</param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (!active || isDead) return;

        float forward = Mathf.Clamp(actions.ContinuousActions[0], 0f, 1f);
        float rotation = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        // Appliquer la rotation
        transform.Rotate(Vector3.up, rotation * rotationSpeed * Time.deltaTime);

        // Appliquer la translation vers l�avant
        transform.position += transform.forward * forward * moveSpeed * Time.deltaTime;
    }

    /// <summary>
    /// D�tecte la collision avec une proie. Capture la proie si contact.
    /// </summary>
    /// <param name="collision">Collision d�tect�e avec un autre objet.</param>
    private void OnCollisionEnter(Collision collision)
    {
        if(isDead) return;

        if (collision.gameObject.CompareTag("Prey"))
        {
            // R�compense positive pour la capture
            AddReward(1f);

            // Notifie l�environnement qu�une proie a �t� attrap�e
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
    }
}
