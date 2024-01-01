module HolidayTracker.Server.Views.Admin.PublishEvent

open HolidayTracker.Shared.Model
open HolidayTracker.Server.Views.Common


let view (dataLevel: int) =
    task {
        return
            html
                $"""
        <link rel="stylesheet" href="/css/publish-events.css?v=202312261741"/>
        <h2> Publish Event</h2>
        <form id="publish-event-form" method="post">
            <label for="date">Target tage</label>
            <input type="date" id=date name=date>
            <label for=title> Title </label>
            <input type="text" id=title name=title placeholder="Public Holiday">
            <label for=body> Body  </label>
               
            <textarea required id=body name="body" placeholder="Public holiday in Argentina"></textarea>
          
            <label for=regions> Regions </label>
            <textarea required name="regions" id=regions placeholder="Argentina, Brazil"></textarea>
           
            <button type="submit">Publish</button>
        </form>
       

        <script defer nonce="110888888">
            document.getElementById('publish-event-form').addEventListener('submit', function(event) {{
                    if (!confirm('Do you really want to submit the form?')) {{
                        event.preventDefault();
                    }}
            }});
        </script>
    """
    }
