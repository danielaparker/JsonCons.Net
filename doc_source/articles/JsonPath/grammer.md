absolute-path = "$" [relative-path]
relative-path = sub-path / bracket-specifier
sub-path = relative-path . (identifier / "*") 

qualified path = recursive-location / relative-location

recursive-location = ".." relative-path

relative-location "." relative-path

relative-path = step qualified-path

