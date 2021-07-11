path = absolute-path / relative-path

absolute-path = "$" [qualified-path]

qualified path = recursive-location / relative-location

recursive-location = ".." relative-path

relative-location "." relative-path

relative-path = step qualified-path

