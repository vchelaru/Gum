class Renderable:

    def draw(self):

        raise NotImplementedError("Subclasses must implement the render method.")
    
    def update(self, delta_time):

        pass