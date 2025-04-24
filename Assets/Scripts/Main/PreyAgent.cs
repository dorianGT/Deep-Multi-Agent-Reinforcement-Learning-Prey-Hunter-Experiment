using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// Agent représentant une proie dans l’environnement.
/// Se déplace pour échapper aux chasseurs et maximise sa survie.
/// </summary>
public class PreyAgent : Agent
{
    /// <summary>
    /// Vitesse de déplacement vers l’avant.
    /// </summary>
    public float moveSpeed = 3f;

    /// <summary>
    /// Vitesse de rotation autour de l’axe Y.
    /// </summary>
    public float rotationSpeed = 200f;

    /// <summary>
    /// Référence vers l’environnement global.
    /// </summary>
    private HunterPreyEnv env;

    /// <summary>
    /// Associe l’environnement à cet agent.
    /// </summary>
    /// <param name="e">L’environnement principal HunterPreyEnv.</param>
    public void SetEnv(HunterPreyEnv e) => env = e;

    /// <summary>
    /// Réinitialisation de l’agent au début d’un épisode.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        // Peut être complété si besoin : repositionner, reset timer, etc.
    }

    /// <summary>
    /// Collecte les observations de l’environnement (à compléter).
    /// Permet à l’agent de percevoir les éléments autour.
    /// </summary>
    /// <param name="sensor">Le capteur d’observations.</param>
    public override void CollectObservations(VectorSensor sensor)
    {
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
        float rotation = actions.ContinuousActions[0];
        float forward = actions.ContinuousActions[1]; 

        // Appliquer la rotation
        transform.Rotate(Vector3.up, rotation * rotationSpeed * Time.deltaTime);

        // Appliquer l'avance
        transform.position += transform.forward * forward * moveSpeed * Time.deltaTime;

        // Récompense pour avoir survécu un pas de temps
        AddReward(0.01f);
    }
}
