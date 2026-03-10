# Sprint 1

**Note:**
L'ouverture du projet nécessite *Unity 6.3 LTS (6000.3.5f2)*

**Accès au fichier principal :**
 - Ouvrir la scène dans *Assets/Scenes/Game.unity*
 - Cliquer sur l'objet *Erosion Testing* dans la hiérarchie
 - Modifier les paramètres au besoin
 - Cliquer sur le bouton *Play* ▶ au dessus de l'écran de la scène

**Pour les contrôles**
 - *E*: appliquer de l'érosion hudraulique
 - *T*: appliquer de l'érosion thermique
 - *F*: appliquer de l'érosion fluviale
 - *R*: réinitialiser le terrain

## Besoins utilisateurs et critères de réussite
**Paramètres de génération (début)**
 
 *Critère de réussite*
 - Pour le moment il est possible de modifier une multitude de paramètres directement dans l'inspecteur de Unity.
 - Par exemple, pour le *Fractal Brownian Motion (FBM)* il est possible de modifier le *scale* et le *offset*, ainsi que d'autres paramètres.

**Transformer des height maps en surfaces 3D**
 
 *Critère de réussite*
 - Pour chaque terrain généré sous format de *Texture EXR*, on crée un *mesh* composé de triangles pour l'afficher en 3D.

**Appliquer de l’érosion (focus principal)**
 
 *Critère de réussite*
 - Plusieurs algorithmes implémentés :
    - *Érosion hydraulique*: Déplacement des sédiments grâce à de multiples gouttes d'eau tombant sur le terrain.
    - *Érosion thermique*: Effritement des collines selon la pente. Imitation des changements de température causant l'effritement.
    - *Érosion fluviale*: Simulation des déversements d'eau dans le terrain pour obtenir des flux hydrauliques réalistes.

**Combiner plusieurs algorithmes (début)**
 
 *Critère de réussite*
 - Il est possible d'appliquer plusieurs itérations d'érosion sur un même terrain
 - Il est aussi possible, quoique cette fonctionalité ne soit pas disponible à l'utilisateur, d'ajouter des *height maps* EXR ensemble pour le combiner.

**Modifier la couleur du terrain**
 
 *Critère de réussite*
 - Des paramètres de couleur sont disponibles pour par exemple choisir quelle couleur apparaît sur le terrain selon quelle hauteur et quelle pente.

**Autres**

Certains besoins utilisateurs tels que la génération par chunks sont aussi inclus dans cette version mais l'interface utilisateur ne permet pas encore de les afficher.

## Intégration des disciplines
En plus, bien évidemment, de la programmation, les mathématiques sont très présentes dans ces premières étapes de conception.
Tout d'abord, certains algorithmes comme le *voronoi* sont basées sur des fonctions de distance (Euclédienne et Manhattan).
Ensuite, les algorithmes d'érosion sont tous dépendants de multiples fonctions de pente, comme par exemple le calcul de *gradient* à partir des cellules voisines.
L'accumulation de sédiments et les dépôts sont aussi entièrement basés sur des équations mathématiques pour assurer la constance de la matière dans le terrain.
