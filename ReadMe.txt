------------------------------------------
---------- SPLINE PREFAB PLACER ----------
------------------------------------------

-----INTRODUCTION-----
This is a guide to the Spline Prefab Placer Tool. This tool is meant to aid when placing several prefabs along a path in a level. The tool allows you to, instead 
of having to place the prefabs one by one, define the path along which they should travel and how many prefabs should be placed along that path. The tool will then 
automatically place the given prefab. For an overview of installation and the main features of the tool, read the Installation guide and quick guide below.

author: Rafael Hoek

-----INSTALLATION-----
  1. Open the unity project in which you want to use this tool
  2. In the toolbar (top of screen), click on 'Assets>Import Package>Custom Package...'
  3. In the file explorer window that pops up, navigate to the "SplinePrefabPlacer.unitypackage"-file, and select it
  4. In the importer window, ensure all files are selected and press "ok"

-----QUICK QUIDE-----
This is a quick start guide, that will guide you through a simple example application of the tool. This is not an exhaustive explanation 
of all the features of the tool, but it will give you the gist. After following the installation steps, do the following:
 1. In a scene where we wish to create a prefab spline, create an empty game-object we'll call "splineTest"
 2. On the splineTest-object, add the component "Prefab Placement Spline". This is a script with a custom inspector window.
 3. Go to the global coordinates 0,0,0. Here you should see two "nodes" (squares); one white and one magenta, and two "control points" (circles)
    - You should also see a line connecting the nodes. This line represents the spline that you will be expanding upon
    - There will be a line connecting each node to either one or two control points. The control points define a direction your spline will take to connect two nodes
    - The reason one point is magenta is because this is the start-node or "first" node of your spline
    - A spline is just a fancy way of saying a 3-dimensional curve. You will be making this curve using the nodes and control points
 4. Select each node in turn and drag them where you wish to place them in your level using the arrows (as you would drag any GameObject in your level)
    - Take some time to play around here. Drag these nodes closer or further, drag the control points, just see what happens when you move each point
    - Place the nodes so that the spline is continuously above the ground
 5. Press the "Add node to end of spline"-button under the header "Spline data". You should see a new node appear. The node will be automatically highlighted
    - Move this node and the new control points around a bit to make an interesting curve
    - If you wish to add a node in the middle of your spline, select the node after which you wish to create a new node and select "Add node here"
 6. We want our curve to be a bit smoother now. Select the central of the three nodes. You should see more options appear under the "spline data"-header
 7. In the "mode"-dropdown menu, select "Mirrored" instead of "Free"
 8. Time to add our prefab(s). Under the header "Prefab Placement Settings", press the small arrow next to "prefabs" to open the list of prefabs that will be placed onto the
    spline. Press the plus-icon to add a new prefab. Then, drag the prefab you want into the "prefab"-GameObject field. Press the plus icon again to add more prefabs.
    - The "weight"-field is only important if you wish to randomly alternate which prefab is placed on the spline. We'll come back to this later.
 9. Now we will select the prefab placement mode from the dropdown list of the same name. This determines how our splines will be placed. We want to place them in a pattern, so 
    we will select the pattern-option. Then, a new field will appear. The tool interprets the letters of the alphabet into indexes of the listed prefabs. Thus, the letter 'A' 
    refers to the first prefab in the list, 'B' to the second, etc... We will enter the pattern "ABBAC" so that we will get a pattern of first element, twice second, first, third.
    It goes without saying that this pattern will only function properly if there are actually 3 elements in the list.
     - The random placement option will place the given prefabs randomly, not following any particular order. This will use the weights given to decide how many of each prefab should
       be included. A higher weight naturally means the prefab will be added more
     - The list option will simply repeat the order given by the list, regardless of weight, pattern, etc...
10. We want our prefabs to be extra long. So under "scale offset" we'll turn the Y-factor of the scale up from 1 to 2. We'll also rotate the prefabs inwards by giving our
    rotation field a Y-factor of 90 degrees. We don't want to introduce any random offsets so we'll leave all those options deselected
11. We do want our prefabs to be placed on the ground, not in the air. To achieve this, we will activate the toggle box for "Project spline onto ground". We will then also 
    activate the toggle box for "Use ground normal" that appears once we have selected the first toggle. This will rotate the prefabs when they are placed so that they are 
    perpendicular to the ground that they are placed on, not perpendicular to the spline
12. Now we'll adjust how many prefabs we want placed on the spline. Let's say we want a prefab every 3 meters, then we write 3 in the "Distance Between Prefabs"-field. If we
    don't know how far appart we want the prefabs, but do know how many we want, for example 20 prefabs, then we write 20 in the "Number of prefabs on spline"-field
13. Finally, we will click on the "Place prefabs on spline"-button. This will place the prefabs we wanted on the spline we have created

Make sure you experiment with this tool a lot more. This quick guide has not thoroughly explored all the possibilities of the tool. Thankfully the tool tends to be pretty 
self-explanatory (I wonder what a random offset is...). The only other feature that bears explaining is the "Resampling settings". What that does is decide how accurately
the prefabs will be placed on the spline. Standard input for this is 0.0001. This means the margin of error for each prefab placement is +/-0.0001 meters. This tends to be
accurate enough for most cases, so actually changing this value is very rarely neccessary. The "Spline Visuals"-section is purely about how the spline looks (making the points bigger to 
make them easier to see). This may be expanded upon eventually but it isn't a priority.
