using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

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

    /// <summary>
    /// Collecte les observations de l�environnement (non utilis� ici).
    /// Peut �tre compl�t� pour ajouter des raycasts ou distances.
    /// </summary>
    /// <param name="sensor">Le capteur utilis� pour collecter les observations.</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        // � compl�ter : distances aux proies, obstacles, raycasts, etc.
    }

    /// <summary>
    /// Applique les actions re�ues par le mod�le ML.
    /// L�agent peut tourner et avancer.
    /// </summary>
    /// <param name="actions">Actions continues : [rotation, avance].</param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (!active) return;

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
        if (other.CompareTag("Danger")) 
        {
            AddReward(-1f);
            env.OnHunterEnterDanger(this);
        }
    }
}
