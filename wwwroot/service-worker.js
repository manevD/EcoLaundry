const CACHE_NAME = "ecolaundry-v1";

const urlsToCache = [
    "/",
    "/offline.html",

    "/css/site.css",
    "/js/site.js",

    "/lib/bootstrap/dist/css/bootstrap.min.css",
    "/lib/bootstrap/dist/js/bootstrap.bundle.min.js",
    "/lib/jquery/dist/jquery.min.js",

    "/android-chrome-192x192.png",
    "/android-chrome-512x512.png"
];


// INSTALL
self.addEventListener("install", event => {

    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => cache.addAll(urlsToCache))
    );

    self.skipWaiting();

});




// ACTIVATE
self.addEventListener("activate", event => {


    event.waitUntil(

        caches.keys().then(keys =>

            Promise.all(

                keys
                    .filter(key => key !== CACHE_NAME)
                    .map(key => caches.delete(key))

            )

        )

    );


    self.clients.claim();


});




// FETCH
self.addEventListener("fetch", event => {



    // само GET кеширај
    if (event.request.method !== "GET") {

        return;

    }



    // слики cache first
    if (event.request.destination === "image") {


        event.respondWith(

            caches.match(event.request)

                .then(cached => {


                    if (cached) {
                        return cached;
                    }


                    return fetch(event.request)

                        .then(response => {


                            return caches.open(CACHE_NAME)

                                .then(cache => {

                                    cache.put(
                                        event.request,
                                        response.clone()
                                    );


                                    return response;


                                });


                        });


                })


        );


        return;

    }







    // страници network first

    event.respondWith(


        fetch(event.request)

            .then(response => {


                return response;


            })


            .catch(() => {


                return caches.match(event.request)

                    .then(cacheResponse => {


                        return cacheResponse || caches.match("/offline.html");


                    });


            })


    );



});