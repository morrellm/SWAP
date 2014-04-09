<html>
    <head>
        <title>PHP script page</title>
    </head>
    <body>
        <a href="<?php echo $_SERVER['PHP_SELF']?>?subject=PHP&web=W3schools.com">Test $GET</a>
        <?php
            include "testPHP2.php";
            $color = "red";
            
            function myFun(){
                $col = "blue";
                echo "myFun()...<br/>\n   ";
                echo "color: $GLOBALS[color]<br/>\n   ";
                echo "col: $col\n\n <br/><br/>  ";
            }
            
            myFun();
            echo "Non function:\n  <br/> ";
            echo "color: $color\n   <br/>";
            echo "col: $col\n";
            echo "OTHER STUFF:<br />";
            echo $_SERVER['PHP_SELF'];
            echo "<br />";
            echo $_SERVER['SERVER_SOFTWARE'];
            echo "<br />";
            echo $_SERVER['HTTP_HOST'];
            echo "<br />";
            echo $_SERVER['HTTP_REFERER'];
            echo "<br />";
            echo $_SERVER['HTTP_USER_AGENT'];
             echo "<br />";
            echo $_SERVER['SERVER_NAME'];
            echo "<br/>";
            echo $_SERVER['SCRIPT_NAME'];
            echo "<br />";
            foreach($_GET as $k => $v){
                echo "Query[$k]: $v <br />";
            }
            
        ?>
        
    </body>
</html>