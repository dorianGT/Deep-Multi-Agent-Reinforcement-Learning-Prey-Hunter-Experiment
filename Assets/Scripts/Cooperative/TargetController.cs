using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contr�leur de la cible dans un environnement d'apprentissage par renforcement.
/// G�re l'interaction avec les agents et d�clenche la r�compense collective
/// lorsque le nombre requis d'agents est atteint.
/// </summary>
public class TargetController : MonoBehaviour
{
    /// <summary>
    /// Ensemble des agents actuellement pr�sents sur la cible.
    /// </summary>
    private HashSet<BasicSensorCooperativeRLAgent> agentsOnTarget = new HashSet<BasicSensorCooperativeRLAgent>();

    /// <summary>
    /// Nombre d'agents requis sur la cible pour d�clencher le succ�s.
    /// </summary>
    public int requiredAgents = 3;

    /// <summary>
    /// Objet visuel utilis� pour indiquer le succ�s (changement de couleur, etc.).
    /// </summary>
    public GameObject feedbackObject;

    /// <summary>
    /// Mat�riau appliqu� � l'objet de feedback en cas de r�ussite (objectif atteint).
    /// </summary>
    public Material winMaterial;

    /// <summary>
    /// Appel� lorsqu�un agent entre dans la zone de la cible.
    /// Ajoute l�agent � la liste et v�rifie si l�objectif est atteint.
    /// </summary>
    /// <param name="agent">L�agent qui entre dans la zone cible.</param>
    public void AgentEntered(BasicSensorCooperativeRLAgent agent)
    {
        agentsOnTarget.Add(agent);
        CheckCompletion();
    }

    /// <summary>
    /// Appel� lorsqu�un agent sort de la zone de la cible.
    /// Retire l�agent de la liste des agents pr�sents.
    /// </summary>
    /// <param name="agent">L�agent qui quitte la zone cible.</param>
    public void AgentExited(BasicSensorCooperativeRLAgent agent)
    {
        agentsOnTarget.Remove(agent);
    }

    /// <summary>
    /// V�rifie si le nombre d�agents pr�sents sur la cible atteint le seuil requis.
    /// Si c�est le cas, attribue une r�compense et termine l��pisode pour les agents concern�s.
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

            // Retire les agents r�compens�s pour �viter les r�compenses r�p�t�es.
            agentsOnTarget.ExceptWith(agentsToReward);
        }
    }

    /// <summary>
    /// Applique un mat�riau � l�objet visuel de feedback pour refl�ter l��tat (succ�s ou �chec).
    /// </summary>
    /// <param name="mat">Mat�riau � appliquer sur l�objet de feedback.</param>
    private void SetFeedbackMaterial(Material mat)
    {
        if (feedbackObject != null && mat != null)
        {
            feedbackObject.GetComponent<Renderer>().material = mat;
        }
    }
}
