# DH2323-AGI---Spider-Man-simulator

# A comparative study of PBD and XPBD soft-body simulation methods to represent web-swinging - DH2323 Final project at KTH
## Abstract
Matthias Müller et al. wrote in 2007 the paper _Position Based Dynamics_[1], detailing a method to simulate soft-bodies with computers. Their research replaces fidelity and realism for robustness and speed of simulation, only taking into account the positions of the points instead of applying sophisticated physical formulas. Another paper, _Extended Position Based Dynamics_[2] by Macklin et al., used that model as a basis to improvr on how constraints are solved. By adding a compliance term, the rope is made more stiff and stable. This project implements both algorithms for a web-slinging simulation, inspired by the character of Spider-Man, while adapting some parts to the context. We moreover apply more forces to the edges of the ropes, whether colliding on surfaces or with the player swinging from the end, and look into the differences between the approaches. Evaluation was done by calculating how far our calculated positions were from ideal ones, those respecting the multiple constraints, and how stable and computationally effective every model is.
This project contains both a free-roaming simulation mode, to swing around and test the dynamics, and fixed scenarios used to evaluate the models' performances.

Supervisor: Prof. Christopher Peters

You may cite this work as:

Kaufman Adam, A comparative study of PBD and XPBD soft-body simulation methods to represent web-swinging (2025). C#. Available: https://github.com/ockedman/DH2323-AGI---Spider-Man-simulator

# Implementation
This study’s implementation uses pre-created objects from the game engine to populate the environment, whereas the controller player and the swinging ropes' movements follow the methods mentioned above. The goal is to find a balance between stability and being able to swing from a point to another. The parameters stay fixed among all scenarios, changing only the objects we interact with, the dynamic modeling approach and when we shoot a web.  The simulation used C# in Unity 6.0.32f1 LTS and Python with the Numpy library to analyse the results.

## Highlights
2025 December 3, main simulation view
<img width="1049" height="544" alt="Capture d&#39;écran 2025-12-03 235413" src="https://github.com/user-attachments/assets/55d63356-93f6-49e3-bde9-64ddba0b2ee3" />

2025, November 30, test scenario
<img width="1038" height="530" alt="Capture d&#39;écran 2025-12-04 000221" src="https://github.com/user-attachments/assets/5a7dafcf-7d66-4039-a660-dfad708676d6" />

# Solving the distance constraint
One of the most crucial parts of our simulation is respecting the distance constraint, e.g. forcing the distance between every pair of points to be constant. The calculations take into account the difference vector and the respective weights, as weightier points move more than lighter ones. If a point reaches a surface, it's considered as "sticky" and cannot move anymore, thus have an almost null weight.
For points $p_i$ and $p_j$, with respective weights of $w_i$ and $w_j$, for a fixed distance of $d$, their correctiones are

<p align="center">$\Delta p_i=-\Delta \lambda_i * w_i*\frac{p_i-p_j}{|p_i-p_j|}$</p>

<p align="center">$\Delta p_j=\Delta \lambda_i * w_j*\frac{p_i-p_j}{|p_i-p_j|}$</p>

with

<p align="center">$\Delta \lambda_i = \frac{|p_i-p_j|-d}{w_i+w_j}$</p>

However, when implementing Extended Position Based Dynamics, we add a compliance term $\alpha$, making that constraint loser. With a time step of $\Delta ts$ and the term $\tilde \alpha = \alpha / \Delta ts ^2$, we get

<p align="center">$\Delta \lambda_i = \frac{|p_i-p_j|-d - \tilde \alpha \lambda_i}{w_i+w_j + \tilde \alpha}$</p>

as each $\lambda_i$ term gets corrected with its gradient at the end of every iteration.

Here is how the code implements it:

`

    public void SolveDistanceConstraint(int i, int j, float ts)
    {
        if (i > pointsThusFar || j > pointsThusFar) return;

        Vector3 delta = currPositions[j] - currPositions[i];
        float dist = delta.magnitude;
        if (dist <= 0.0001f) return;

        float w1 = GetWeight(i);
        float w2 = GetWeight(j);
        float wSum = w1 + w2;
        if (wSum <= 0f) return;

        float C = -dist + actualDistance;
        Vector3 n = delta / dist;
        float deltaLambda = 0f;

        if (GlobalParameters.instance.methode == GlobalParameters.Methods.PBD)
        {
            deltaLambda = -C / wSum;
        }

        else if (GlobalParameters.instance.methode == GlobalParameters.Methods.XPBD)
        {
            float alphats = compliance / (ts * ts);
            deltaLambda = -(C + alphats * lambdasDist[i]) / (wSum + alphats);

            lambdasDist[i] += deltaLambda;
        }

        Vector3 correctioni = deltaLambda * w1 * n;
        Vector3 correctionj = -deltaLambda * w2 * n;

        currPositions[i] += correctioni;
        currPositions[j] += correctionj;

        if (i == 0 && isAttached && hasReached)
        {
            Vector3 playerPos = player.GetCurrPos();
            float ratio = 1f;
            player.ropeForce += correctioni * ratio / (ts * ts) * playerMass;
            player.currPos += correctioni * (1f - ratio);

        }
    }


The last part serves to also move the **Player** object, as it may have moved too during the iteration into a separate direction and has to be brought back with the point.


# References
[1] Müller, M., Heidelberger, B., Hennix, M., & Ratcliff, J. (2007). Position based dynamics. _Journal of Visual Communication and Image Representation_, 18(2), 109-118.
[2] Macklin, M., Müller, M., & Chentanez, N. (2016, October). XPBD: position-based simulation of compliant constrained dynamics. In _Proceedings of the 9th International Conference on Motion in Games_ (pp. 49-54).
