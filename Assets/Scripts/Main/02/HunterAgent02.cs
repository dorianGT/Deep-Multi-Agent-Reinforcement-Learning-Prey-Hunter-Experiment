using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// Agent contrôlé par ML-Agents représentant un chasseur dans l'environnement.
/// Ce chasseur peut se déplacer et tourner selon les actions reçues,
/// et interagir avec les proies pour les capturer.
/// </summary>
public class HunterAgent02 : Agent
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

    /// <summary>
    /// Référence vers l’environnement principal, pour signaler les événements.
    /// </summary>
    private HunterPreyEnv02 env;

    /// <summary>
    /// Attribue l'environnement au chasseur.
    /// </summary>
    /// <param name="e">L’environnement principal HunterPreyEnv.</param>
    public void SetEnv(HunterPreyEnv02 e) => env = e;

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

    /// <summary>
    /// Collecte les observations de l’environnement (non utilisé ici).
    /// Peut être complété pour ajouter des raycasts ou distances.
    /// </summary>
    /// <param name="sensor">Le capteur utilisé pour collecter les observations.</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        // À compléter : distances aux proies, obstacles, raycasts, etc.
    }

    /// <summary>
    /// Applique les actions reçues par le modèle ML.
    /// L’agent peut tourner et avancer.
    /// </summary>
    /// <param name="actions">Actions continues : [rotation, avance].</param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (!active) return;

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
        if (other.CompareTag("Danger")) 
        {
            AddReward(-1f);
            env.OnHunterEnterDanger(this);
        }
    }
}
