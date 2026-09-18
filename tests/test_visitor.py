import os
import unittest
import subprocess
from concurrent.futures import ThreadPoolExecutor
import test_ownership

test_ownership.BASE = os.environ.get("VISITOR_API_URL", "http://127.0.0.1:5215")
request = test_ownership.request

class VisitorTests(test_ownership.OwnershipTests):
    def setUp(self):
        status, first = request("/demo/session", "POST")
        self.assertEqual(status, 200)
        status, second = request("/demo/session", "POST")
        self.assertEqual(status, 200)
        self.alice, self.bob = first["accessToken"], second["accessToken"]
        self.alice_subject, self.bob_subject = first["subject"], second["subject"]
    def tearDown(self):
        request("/demo/session", "DELETE", token=self.alice)
        request("/demo/session", "DELETE", token=self.bob)
    def test_reset_revokes_session_and_preserves_other_visitor(self):
        self.assertEqual(request("/Company", "POST", {"name":"Synthetic"}, self.alice)[0], 201)
        self.assertEqual(request("/demo/session", "DELETE", token=self.alice)[0], 204)
        self.assertEqual(request("/Company", token=self.alice)[0], 401)
        self.assertEqual(request("/Company", token=self.bob)[0], 200)
        self.assertEqual(request("/dev/token/alice")[0], 404)


    @unittest.skipIf(os.environ.get("VISITOR_API_URL"), "Direct database checks use the local disposable container only")
    def test_reset_serializes_writes_and_removes_rows(self):
        self.assertEqual(request("/Company", "POST", {"name":"Before reset"}, self.alice)[0], 201)
        with ThreadPoolExecutor(max_workers=3) as pool:
            writes = [pool.submit(request, "/Company", "POST", {"name":"Concurrent sample"}, self.alice) for _ in range(2)]
            reset = pool.submit(request, "/demo/session", "DELETE", None, self.alice)
            self.assertEqual(reset.result()[0], 204)
            for write in writes: self.assertIn(write.result()[0], (201, 401))
        self.assertEqual(self.sql(f'SELECT count(*) FROM "Companies" WHERE "UserId" = $subject${self.alice_subject}$subject$'), "0")
        self.assertEqual(self.sql(f'SELECT count(*) FROM "VisitorSessions" WHERE "Id" = $subject${self.alice_subject}$subject$'), "0")

    @unittest.skipIf(os.environ.get("VISITOR_API_URL"), "Expiry manipulation is local only")
    def test_database_expiry_rejects_existing_token(self):
        self.sql(f'UPDATE "VisitorSessions" SET "ExpiresAt" = now() - interval $$1 second$$ WHERE "Id" = $subject${self.alice_subject}$subject$')
        self.assertEqual(request("/Company", token=self.alice)[0], 401)
        self.assertEqual(request("/Company", "POST", {"name":"Expired"}, self.alice)[0], 401)
        # Restore the test session expiry so normal teardown also proves physical reset.
        self.sql(f'UPDATE "VisitorSessions" SET "ExpiresAt" = now() + interval $$1 minute$$ WHERE "Id" = $subject${self.alice_subject}$subject$')

    @staticmethod
    def sql(query):
        result = subprocess.run(["docker", "exec", "tofu-jobs-visitor-database-1", "psql", "-U", "visitor", "-d", "jobs_visitor", "-tAc", query], capture_output=True, text=True, check=True)
        return result.stdout.strip()

if __name__ == "__main__": unittest.main(verbosity=2)
