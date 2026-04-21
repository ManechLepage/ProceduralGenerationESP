# Sprint 2

**Note:**
L'ouverture du projet nécessite *Unity 6.3 LTS (6000.3.5f2)*

**Accès au fichier principal :**
 - Ouvrir la scène dans *Assets/Scenes/Game.unity*
 - Cliquer sur le bouton *Play* ▶ au dessus de l'écran de la scène

## Besoins utilisateurs et critères de réussite
**Paramètres de génération (interface)**
 
 *Critère de réussite*
 - Pour le moment il est possible de modifier une multitude de paramètres directement dans une interface utilisateur
 - Un réseau de nodes permet de créer du terrain en utilisant plusieurs algorithmes, ainsi que de combiner les terrains et appliquer de l'érosion

**Exportation dans un jeu (Minecraft)**
 
 *Critère de réussite*
 - Pour pouvoir visualiser mieux la génération dans un contexte réel de son utilisation, nous avons créé un node dans l'interface pour exporter le terrain dans Minecraft.
 - Il est possible de modifier le bloc palette et la taille du terrain exporté.

**Visualisation facile et interactive**
 
 *Critère de réussite*
 - Il est possible d'ajouter des drapeaux sur certains nodes pour afficher le terrain généré à un certain point de sa génération.
 - En appuyant sur espace, on peut faire pause et on peut relancer la génération.

**Prévision du temps de génération**
 
 *Critère de réussite*
 - En utilisant les données du temps de génération, il a été possible de déterminer la complexité computationnelle de la génération de plusieurs algorithmes selon leurs paramètres, pour ainsi prédire le temps total de génération.


## Intégration des disciplines
Pour la prévision des résultats, nous avons utilisé un fichier excel pour estimer les courbes du temps de génération en fonction de la valeur de chaque paramètre, ce qui a permit de déterminer le temps de génération selon la complexité ajoutée par chaque paramètre.
