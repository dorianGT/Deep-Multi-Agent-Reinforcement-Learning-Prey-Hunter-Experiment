using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contrôleur de la cible dans un environnement d'apprentissage par renforcement.
/// Gère l'interaction avec les agents et déclenche la récompense collective
/// lorsque le nombre requis d'agents est atteint.
/// </summary>
public class TargetController : MonoBehaviour
{
    /// <summary>
    /// Ensemble des agents actuellement présents sur la cible.
    /// </summary>
    private HashSet<BasicSensorCooperativeRLAgent> agentsOnTarget = new HashSet<BasicSensorCooperativeRLAgent>();

    /// <summary>
    /// Nombre d'agents requis sur la cible pour déclencher le succès.
    /// </summary>
    public int requiredAgents = 3;

    /// <summary>
    /// Objet visuel utilisé pour indiquer le succès (changement de couleur, etc.).
    /// </summary>
    public GameObject feedbackObject;

    /// <summary>
    /// Matériau appliqué à l'objet de feedback en cas de réussite (objectif atteint).
    /// </summary>
    public Material winMaterial;

    /// <summary>
    /// Appelé lorsqu’un agent entre dans la zone de la cible.
    /// Ajoute l’agent à la liste et vérifie si l’objectif est atteint.
    /// </summary>
    /// <param name="agent">L’agent qui entre dans la zone cible.</param>
    public void AgentEntered(BasicSensorCooperativeRLAgent agent)
    {
        agentsOnTarget.Add(agent);
        CheckCompletion();
    }

    /// <summary>
    /// Appelé lorsqu’un agent sort de la zone de la cible.
    /// Retire l’agent de la liste des agents présents.
    /// </summary>
    /// <param name="agent">L’agent qui quitte la zone cible.</param>
    public void AgentExited(BasicSensorCooperativeRLAgent agent)
    {
        agentsOnTarget.Remove(agent);
    }

    /// <summary>
    /// Vérifie si le nombre d’agents présents sur la cible atteint le seuil requis.
    /// Si c’est le cas, attribue une récompense et termine l’épisode pour les agents concernés.
    /// </summary>
    private void CheckCompletion()
    {
        if (agentsOnTarget.Count >= requiredAgents)
        {
            SetFeedbackMaterial(winMaterial);
            var agentsToReward = new List<BasicSensorCooperativeRLAgent>(agentsOnTarget);

            foreach (var agent in agentsToReward)
            {
                agent.AddReward(1.0f);
                agent.EndEpisode();
            }

            // Retire les agents récompensés pour éviter les récompenses répétées.
            agentsOnTarget.ExceptWith(agentsToReward);
        }
    }

    /// <summary>
    /// Applique un matériau à l’objet visuel de feedback pour refléter l’état (succès ou échec).
    /// </summary>
    /// <param name="mat">Matériau à appliquer sur l’objet de feedback.</param>
    private void SetFeedbackMaterial(Material mat)
    {
        if (feedbackObject != null && mat != null)
        {
            feedbackObject.GetComponent<Renderer>().material = mat;
        }
    }
}
