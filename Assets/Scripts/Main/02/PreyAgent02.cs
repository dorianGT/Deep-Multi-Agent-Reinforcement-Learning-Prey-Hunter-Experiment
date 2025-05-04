using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// Agent repr�sentant une proie dans l�environnement.
/// Se d�place pour �chapper aux chasseurs et maximise sa survie.
/// </summary>
public class PreyAgent02 : Agent
{
    /// <summary>
    /// Vitesse de d�placement vers l�avant.
    /// </summary>
    public float moveSpeed = 3f;

    /// <summary>
    /// Vitesse de rotation autour de l�axe Y.
    /// </summary>
    public float rotationSpeed = 200f;

    private float energy;
    public float minEnergy = 5;
    public float maxEnergy = 15f;
    public float energyDecayRate = 1f; // �nergie perdue par seconde
    public float energyGainAmount = 5f; // �nergie gagn�e en touchant une EnergyPrey

    /// <summary>
    /// R�f�rence vers l�environnement global.
    /// </summary>
    private HunterPreyEnv02 env;

    /// <summary>
    /// Associe l�environnement � cet agent.
    /// </summary>
    /// <param name="e">L�environnement principal HunterPreyEnv.</param>
    public void SetEnv(HunterPreyEnv02 e) => env = e;

    /// <summary>
    /// R�initialisation de l�agent au d�but d�un �pisode.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        energy = Random.Range(minEnergy, maxEnergy);
        // Peut �tre compl�t� si besoin : repositionner, reset timer, etc.
    }

    /// <summary>
    /// Collecte les observations de l�environnement (� compl�ter).
    /// Permet � l�agent de percevoir les �l�ments autour.
    /// </summary>
    /// <param name="sensor">Le capteur d�observations.</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(energy / maxEnergy); // Normalis� entre 0 et 1
        // � compl�ter : distances aux chasseurs, raycasts, murs proches, etc.
    }

    /// <summary>
    /// Re�oit et applique les actions continues.
    /// La proie peut tourner et avancer.
    /// Une petite r�compense est donn�e � chaque frame pour survivre plus longtemps.
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

        // R�compense pour avoir surv�cu un pas de temps
        AddReward(0.01f);

        // Diminution de l'�nergie
        energy -= energyDecayRate * Time.deltaTime;

        // Punition si l'�nergie tombe � z�ro
        if (energy <= 0f)
        {
            SetReward(-1f); // ou une r�compense personnalis�e
            env.OnPreyEnergyDepleted(this); // � impl�menter dans l�environnement
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
