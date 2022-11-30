# Simmakers Interview Test Solution

## How-To

1. Clone the repo
2. Open the solution with Visual Studio Code
3. Run it with docker-compose
4. Wait for the browser window to open
5. Check out the user list on the index page
6. Click on "Register" in the top right corner
7. Enter your credentials
8. Press "Register" button
9. Confirm your email
10. Click "Login in the top right corner
11. Enter your credentials
12. Press "Log in" button
13. Click on your email in the top right corner to get to the account management page
14. Click on "Manage avatar" in the left navbar to get to the avatar management page
15. Checkout out the avatar upload functionality
16. Upload some avatars and press "Select" button to use it

## Notes

Storing blobs in an RDBMS leads to serious maintenance issues: your tables grow to unmanageable sizes pretty fast: any schema changes may lock your database for hours.
Therefore, doing so is justifiably considered a bad practice. You can bypass the problem by putting the blob column in a separate table, but this is just a half-measure. The only one good option for storing blobs is a dedicated storage. This solution utilizes an S3-compatible storage - Minio.

Most important files:

1. /Areas/Identity/Services/ScopedFileManager.cs encapsulates blob storage logic.
2. /Areas/Identity/Pages/Account/File.cshtml allows to download a blob.
3. /Areas/Identity/Pages/Account/Manage/ManageAvatar.cshtml allows to upload or delete a blob (picture).
4. /Areas/Identity/Pages/Account/Manage/DeletePersonalData.cshtml is modified to delete blobs uploaded by the user as well as the rest of his/her personal data.
5. /Pages/Index.cshtml is modified to display existing users.