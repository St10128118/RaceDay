# API Endpoint Table

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|-------------|--------|-------------|----------------|--------------|-------------------|
| POST | /api/v1/auth/register | Creates a new user account with role (Organiser or Participant). | None (public) | { firstName, lastName, email, password, role, ... } | 201 Created – User registered. 400 Bad Request – Invalid data. 409 Conflict – Email exists. |
| POST | /api/v1/auth/login | Authenticates a user and returns a JWT token. | None (public) | { email, password } | 200 OK – Token returned. 401 Unauthorized – Invalid credentials. |
| GET | /api/v1/users/me | Returns the authenticated user's profile. | Any (logged in) | N/A | 200 OK – User profile. 401 Unauthorized – Not logged in. |
| PUT | /api/v1/users/me | Updates the authenticated user's profile. | Any (logged in) | { firstName?, lastName?, phone?, emergencyContact?, organisationName? } | 200 OK – Updated profile. 400 Bad Request – Invalid fields. |
| GET | /api/v1/events | Returns all events (filterable). | None (public) | N/A | 200 OK – List of events. |
| GET | /api/v1/events/{eventId} | Returns details of a single event. | None (public) | N/A | 200 OK – Event details. 404 Not Found. |
| POST | /api/v1/events | Creates a new event. | Organiser only | { name, description, eventType, location, city, province, distanceKm, eventDateTime, maxParticipants } | 201 Created – Event created. 400 Bad Request. |
| PUT | /api/v1/events/{eventId} | Updates an existing event. | Organiser only | { name?, description?, eventType?, ... } | 200 OK – Updated. 403 Forbidden – Not owner. |
| DELETE | /api/v1/events/{eventId} | Deletes an event. | Organiser only | N/A | 204 No Content – Deleted. 403 Forbidden – Not owner. |
| GET | /api/v1/events/{eventId}/categories | Returns all categories for an event. | None (public) | N/A | 200 OK – List of categories. |
| GET | /api/v1/events/{eventId}/categories/{categoryId} | Returns a single category. | None (public) | N/A | 200 OK – Category details. |
| POST | /api/v1/events/{eventId}/categories | Creates a category for an event. | Organiser only | { name, distanceKm?, minAge?, maxAge? } | 201 Created. |
| PUT | /api/v1/events/{eventId}/categories/{categoryId} | Updates a category. | Organiser only | { name?, distanceKm?, minAge?, maxAge? } | 200 OK – Updated. |
| DELETE | /api/v1/events/{eventId}/categories/{categoryId} | Deletes a category. | Organiser only | N/A | 204 No Content. |
| GET | /api/v1/events/{eventId}/enrolments | Returns all enrolments for an event. | Organiser only | N/A | 200 OK – List of enrolments. |
| GET | /api/v1/enrolments/{enrolmentId} | Returns a single enrolment. | Any (logged in) | N/A | 200 OK – Enrolment details. 403 Forbidden – If not owner. |
| POST | /api/v1/events/{eventId}/enrolments | Creates a new enrolment for a participant. | Participant only | { categoryId } | 201 Created – Enrolment created. |
| PUT | /api/v1/enrolments/{enrolmentId} | Updates participant enrolment (e.g., confirm payment). | Participant only | { paymentStatus?, bibNumber? } | 200 OK – Updated. |
| DELETE | /api/v1/enrolments/{enrolmentId} | Cancels participant enrolment. | Participant only | N/A | 204 No Content. |
| GET | /api/v1/events/{eventId}/results | Returns all results for an event. | None (public) | N/A | 200 OK – List of results. |
| GET | /api/v1/enrolments/{enrolmentId}/result | Returns the result for a specific enrolment. | Any (logged in) | N/A | 200 OK – Result. |
| POST | /api/v1/enrolments/{enrolmentId}/result | Creates a result for a participant. | Organiser only | { finishTimeSeconds, positionOverall, positionCategory, positionGender, status } | 201 Created. |
| PUT | /api/v1/results/{resultId} | Updates a result. | Organiser only | { finishTimeSeconds?, positionOverall?, ... } | 200 OK – Updated. |
| DELETE | /api/v1/results/{resultId} | Deletes a result. | Organiser only | N/A | 204 No Content. |
| GET | /api/v1/events/{eventId}/routes | Returns all routes for an event. | None (public) | N/A | 200 OK – List of routes. |
| GET | /api/v1/routes/{routeId} | Returns a single route. | None (public) | N/A | 200 OK – Route details. |
| POST | /api/v1/events/{eventId}/routes | Creates a route for an event. | Organiser only | { name, distanceKm, gpxUrl?, elevationGainMeters? } | 201 Created. |
| PUT | /api/v1/routes/{routeId} | Updates a route. | Organiser only | { name?, distanceKm?, gpxUrl?, elevationGainMeters? } | 200 OK – Updated. |
| DELETE | /api/v1/routes/{routeId} | Deletes a route. | Organiser only | N/A | 204 No Content. |
| GET | /api/v1/users/me/history | Returns the authenticated participant’s full race history. | Participant only | N/A | 200 OK – List of events + results. |