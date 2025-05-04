using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// Agent représentant une proie dans l’environnement.
/// Se déplace pour échapper aux chasseurs et maximise sa survie.
/// </summary>
public class PreyAgent02 : Agent
{
    /// <summary>
    /// Vitesse de déplacement vers l’avant.
    /// </summary>
    public float moveSpeed = 3f;

    /// <summary>
    /// Vitesse de rotation autour de l’axe Y.
    /// </summary>
    public float rotationSpeed = 200f;

    private float energy;
    public float minEnergy = 5;
    public float maxEnergy = 15f;
    public float energyDecayRate = 1f; // énergie perdue par seconde
    public float energyGainAmount = 5f; // énergie gagnée en touchant une EnergyPrey

    /// <summary>
    /// Référence vers l’environnement global.
    /// </summary>
    private HunterPreyEnv02 env;

    /// <summary>
    /// Associe l’environnement à cet agent.
    /// </summary>
    /// <param name="e">L’environnement principal HunterPreyEnv.</param>
    public void SetEnv(HunterPreyEnv02 e) => env = e;

    /// <summary>
    /// Réinitialisation de l’agent au début d’un épisode.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        energy = Random.Range(minEnergy, maxEnergy);
        // Peut être complété si besoin : repositionner, reset timer, etc.
    }

    /// <summary>
    /// Collecte les observations de l’environnement (à compléter).
    /// Permet à l’agent de percevoir les éléments autour.
    /// </summary>
    /// <param name="sensor">Le capteur d’observations.</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(energy / maxEnergy); // Normalisé entre 0 et 1
        // À compléter : distances aux chasseurs, raycasts, murs proches, etc.
    }

    /// <summary>
    /// Reçoit et applique les actions continues.
    /// La proie peut tourner et avancer.
    /// Une petite récompense est donnée à chaque frame pour survivre plus longtemps.
    /// </summary>
    /// <param name="actions">Actions continues : [rotation, mouvement].</param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        float forward = Mathf.Clamp(actions.ContinuousActions[0], 0f, 1f);
        float rotation = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        // Appliquer la rotation
        transform.Rotate(Vector3.up, rotation * rotationSpeed * Time.deltaTime);

        // Appliquer l'avance
        transform.position += transform.forward * forward * moveSpeed * Time.deltaTime;

        // Récompense pour avoir survécu un pas de temps
        AddReward(0.01f);

        // Diminution de l'énergie
        energy -= energyDecayRate * Time.deltaTime;

        // Punition si l'énergie tombe à zéro
        if (energy <= 0f)
        {
            SetReward(-1f); // ou une récompense personnalisée
            env.OnPreyEnergyDepleted(this); // à implémenter dans l’environnement
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            SetReward(-1f);
            env.OnPreyEnterDanger(this);
        }

        if (collision.gameObject.CompareTag("Obstacle"))
        {
            AddReward(-0.2f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Danger"))
        {
            SetReward(-1f);
            env.OnPreyEnterDanger(this);
        }
        else if (other.CompareTag("EnergyPrey"))
        {
            energy = Mathf.Min(energy + energyGainAmount, maxEnergy);
            AddReward(1f);
            env.OnPreyEnergy(other.gameObject);
        }
    }
}
